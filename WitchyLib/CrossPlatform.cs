using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.FileSystemGlobbing;

namespace WitchyLib;

public static class BndPath
{

    private static readonly Regex DriveRx = new Regex(@"^(\w\:\\)(.+)$");
    private static readonly Regex TraversalRx = new Regex(@"^([(..)\\\/]+)(.+)?$");
    private static readonly Regex SlashRx = new Regex(@"^(\\+)(.+)$");

    public static string Combine(params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        return Combine((ReadOnlySpan<string>)paths);
    }

    /// <summary>
    /// Combines a span of strings into a path.
    /// </summary>
    /// <param name="paths">A span of parts of the path.</param>
    /// <returns>The combined paths.</returns>
    public static string Combine(params ReadOnlySpan<string> paths)
    {
        int maxSize = 0;
        int firstComponent = 0;

        // We have two passes, the first calculates how large a buffer to allocate and does some precondition
        // checks on the paths passed in.  The second actually does the combination.

        for (int i = 0; i < paths.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(paths[i], nameof(paths));

            if (paths[i].Length == 0)
            {
                continue;
            }

            if (IsPathRooted(paths[i]))
            {
                firstComponent = i;
                maxSize = paths[i].Length;
            }
            else
            {
                maxSize += paths[i].Length;
            }

            char ch = paths[i][^1];
            if (!IsDirectorySeparator(ch))
                maxSize++;
        }

        var builder = new StringBuilder(); // MaxShortPath on Windows
        builder.EnsureCapacity(maxSize);

        for (int i = firstComponent; i < paths.Length; i++)
        {
            if (paths[i].Length == 0)
            {
                continue;
            }

            if (builder.Length == 0)
            {
                builder.Append(paths[i]);
            }
            else
            {
                char ch = builder[^1];
                if (!IsDirectorySeparator(ch))
                {
                    builder.Append('\\');
                }

                builder.Append(paths[i]);
            }
        }

        return builder.ToString();
    }

    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileName(string? path)
    {
        if (path == null)
            return null;

        ReadOnlySpan<char> result = GetFileName(path.AsSpan());
        if (path.Length == result.Length)
            return path;

        return result.ToString();
    }

    internal static bool IsDirectorySeparator(char c)
    {
        return c == '\\';
    }

    /// <summary>
    /// Normalize separators in the given path. Compresses forward slash runs.
    /// </summary>
    [return: NotNullIfNotNull(nameof(path))]
    internal static string? NormalizeDirectorySeparators(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Make a pass to see if we need to normalize so we can potentially skip allocating
        bool normalized = true;

        for (int i = 0; i < path.Length; i++)
        {
            if (IsDirectorySeparator(path[i])
                && (i + 1 < path.Length && IsDirectorySeparator(path[i + 1])))
            {
                normalized = false;
                break;
            }
        }

        if (normalized)
            return path;

        StringBuilder builder = new StringBuilder(path.Length);

        for (int i = 0; i < path.Length; i++)
        {
            char current = path[i];

            // Skip if we have another separator following
            if (IsDirectorySeparator(current)
                && (i + 1 < path.Length && IsDirectorySeparator(path[i + 1])))
                continue;

            builder.Append(current);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns the directory portion of a file path. This method effectively
    /// removes the last segment of the given file path, i.e. it returns a
    /// string consisting of all characters up to but not including the last
    /// backslash ("\") in the file path. The returned value is null if the
    /// specified path is null, empty, or a root (such as "\", "C:", or
    /// "\\server\share").
    /// </summary>
    /// <remarks>
    /// Directory separators are normalized in the returned string.
    /// </remarks>
    public static string? GetDirectoryName(string? path)
    {
        if (path == null || IsEffectivelyEmpty(path.AsSpan()))
            return null;

        int end = GetDirectoryNameOffset(path.AsSpan());
        return end >= 0 ? NormalizeDirectorySeparators(path.Substring(0, end)) : null;
    }

    /// <summary>
    /// Returns the directory portion of a file path. The returned value is empty
    /// if the specified path is null, empty, or a root (such as "\", "C:", or
    /// "\\server\share").
    /// </summary>
    /// <remarks>
    /// Unlike the string overload, this method will not normalize directory separators.
    /// </remarks>
    public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path)
    {
        if (IsEffectivelyEmpty(path))
            return ReadOnlySpan<char>.Empty;

        int end = GetDirectoryNameOffset(path);
        return end >= 0 ? path.Slice(0, end) : ReadOnlySpan<char>.Empty;
    }

    internal static int GetRootLength(ReadOnlySpan<char> path)
    {
        return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
    }

    internal static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
    {
        int rootLength = GetRootLength(path);
        int end = path.Length;
        if (end <= rootLength)
            return -1;

        while (end > rootLength && !IsDirectorySeparator(path[--end])) ;

        // Trim off any remaining separators (to deal with C:\foo\\bar)
        while (end > rootLength && IsDirectorySeparator(path[end - 1]))
            end--;

        return end;
    }

    /// <summary>
    /// Returns true if the path is effectively empty for the current OS.
    /// For unix, this is empty or null. For Windows, this is empty, null, or
    /// just spaces ((char)32).
    /// </summary>
    internal static bool IsEffectivelyEmpty(string? path)
    {
        return string.IsNullOrEmpty(path);
    }

    internal static bool IsEffectivelyEmpty(ReadOnlySpan<char> path)
    {
        return path.IsEmpty;
    }

    /// <summary>
    /// Returns the path root or null if path is empty or null.
    /// </summary>
    public static string? GetPathRoot(string? path)
    {
        if (IsEffectivelyEmpty(path)) return null;
        return IsPathRooted(path) ? "\\" : string.Empty;
    }

    public static bool IsPathRooted(ReadOnlySpan<char> path)
    {
        return path.StartsWith('\\');
    }

    public static ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path)
    {
        return IsPathRooted(path) ? "\\".AsSpan() : ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// The returned ReadOnlySpan contains the characters of the path that follows the last separator in path.
    /// </summary>
    public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
    {
        int root = GetPathRoot(path).Length;

        // We don't want to cut off "C:\file.txt:stream" (i.e. should be "file.txt:stream")
        // but we *do* want "C:Foo" => "Foo". This necessitates checking for the root.

        int i = path.LastIndexOf("\\");

        return path.Slice(i < root ? root : i + 1);
    }

    private static string RemoveLeadingBackslashes(string path)
    {
        Match slash = SlashRx.Match(path);
        if (slash.Success)
        {
            path = slash.Groups[2].Value;
        }

        return path;
    }

    /// <summary>
    /// Removes common network path roots if present.
    /// </summary>
    public static string Unroot(string path, string root)
    {
        path = path.Substring(root?.Length ?? 0);

        Match drive = DriveRx.Match(path);

        if (drive.Success)
        {
            path = drive.Groups[2].Value;
        }

        if (string.IsNullOrWhiteSpace(root))
            return RemoveLeadingBackslashes(path);

        Match traversal = TraversalRx.Match(path);
        if (traversal.Success)
        {
            path = traversal.Groups[2].Value;
        }

        if (path.Contains("..\\") || path.Contains("../"))
            throw new InvalidDataException(
                $"the path {path} contains invalid data, attempting to extract to a different folder.");
        return RemoveLeadingBackslashes(path);
    }

    /// <summary>
    /// Finds common path prefix in a list of strings.
    /// </summary>
    public static string FindCommonBndRootPath(IEnumerable<string> paths)
    {
        string root = "";

        if (paths.Count() == 0) return root;

        var rootPath = new string(
            paths.First().Substring(0, paths.Min(s => s.Length))
                .TakeWhile((c, i) => paths.All(s => s[i] == c)).ToArray());

        // For safety, truncate this shared string down to the last slash/backslash.
        var rootPathIndex = Math.Max(rootPath.LastIndexOf('\\'), rootPath.LastIndexOf('/'));

        if (rootPath != "" && rootPathIndex != -1)
        {
            root = rootPath.Substring(0, rootPathIndex);
        }

        return root;
    }

    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileNameWithoutExtension(string? path)
    {
        if (path == null)
            return null;

        ReadOnlySpan<char> result = GetFileNameWithoutExtension(path.AsSpan());
        if (path.Length == result.Length)
            return path;

        return result.ToString();
    }

    /// <summary>
    /// Returns the characters between the last separator and last (.) in the path.
    /// </summary>
    public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
    {
        ReadOnlySpan<char> fileName = GetFileName(path);
        int lastPeriod = fileName.LastIndexOf('.');
        return lastPeriod < 0
            ? fileName
            : // No extension was found
            fileName.Slice(0, lastPeriod);
    }

    /// <summary>
    /// Returns the extension of the given path. The returned value includes the period (".") character of the
    /// extension except when you have a terminal period when you get string.Empty, such as ".exe" or ".cpp".
    /// The returned value is null if the given path is null or empty if the given path does not include an
    /// extension.
    /// </summary>
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetExtension(string? path)
    {
        if (path == null)
            return null;

        return GetExtension(path.AsSpan()).ToString();
    }

    /// <summary>
    /// Returns the extension of the given path.
    /// </summary>
    /// <remarks>
    /// The returned value is an empty ReadOnlySpan if the given path does not include an extension.
    /// </remarks>
    public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
    {
        int length = path.Length;

        for (int i = length - 1; i >= 0; i--)
        {
            char ch = path[i];
            if (ch == '.')
            {
                if (i != length - 1)
                    return path.Slice(i, length - i);
                else
                    return ReadOnlySpan<char>.Empty;
            }

            if (IsDirectorySeparator(ch))
                break;
        }

        return ReadOnlySpan<char>.Empty;
    }

    // Changes the extension of a file path. The path parameter
    // specifies a file path, and the extension parameter
    // specifies a file extension (with a leading period, such as
    // ".exe" or ".cs").
    //
    // The function returns a file path with the same root, directory, and base
    // name parts as path, but with the file extension changed to
    // the specified extension. If path is null, the function
    // returns null. If path does not contain a file extension,
    // the new file extension is appended to the path. If extension
    // is null, any existing extension is removed from path.
    [return: NotNullIfNotNull(nameof(path))]
    public static string? ChangeExtension(string? path, string? extension)
    {
        if (path == null)
            return null;

        int subLength = path.Length;
        if (subLength == 0)
            return string.Empty;

        for (int i = path.Length - 1; i >= 0; i--)
        {
            char ch = path[i];

            if (ch == '.')
            {
                subLength = i;
                break;
            }

            if (IsDirectorySeparator(ch))
            {
                break;
            }
        }

        if (extension == null)
        {
            return path.Substring(0, subLength);
        }

        ReadOnlySpan<char> subpath = path.AsSpan(0, subLength);
        return extension.StartsWith('.') ? string.Concat(subpath, extension) : string.Concat(subpath, ".", extension);
    }

    /// <summary>
    /// Gets the count of common characters from the left optionally ignoring case
    /// </summary>
    internal static unsafe int EqualStartingCharacterCount(string? first, string? second, bool ignoreCase)
    {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second)) return 0;

        int commonChars = 0;

        fixed (char* f = first)
        fixed (char* s = second)
        {
            char* l = f;
            char* r = s;
            char* leftEnd = l + first.Length;
            char* rightEnd = r + second.Length;

            while (l != leftEnd && r != rightEnd
                                && (*l == *r || (ignoreCase && char.ToUpperInvariant(*l) == char.ToUpperInvariant(*r))))
            {
                commonChars++;
                l++;
                r++;
            }
        }

        return commonChars;
    }

    /// <summary>
    /// Get the common path length from the start of the string.
    /// </summary>
    internal static int GetCommonPathLength(string first, string second, bool ignoreCase)
    {
        int commonChars = EqualStartingCharacterCount(first, second, ignoreCase: ignoreCase);

        // If nothing matches
        if (commonChars == 0)
            return commonChars;

        // Or we're a full string and equal length or match to a separator
        if (commonChars == first.Length
            && (commonChars == second.Length || IsDirectorySeparator(second[commonChars])))
            return commonChars;

        if (commonChars == second.Length && IsDirectorySeparator(first[commonChars]))
            return commonChars;

        // It's possible we matched somewhere in the middle of a segment e.g. C:\Foodie and C:\Foobar.
        while (commonChars > 0 && !IsDirectorySeparator(first[commonChars - 1]))
            commonChars--;

        return commonChars;
    }

    /// <summary>
    /// Create a relative path from one path to another. Paths will be resolved before calculating the difference.
    /// Default path comparison for the active platform will be used (OrdinalIgnoreCase for Windows or Mac, Ordinal for Unix).
    /// </summary>
    /// <param name="relativeTo">The source path the output should be relative to. This path is always considered to be a directory.</param>
    /// <param name="path">The destination path.</param>
    /// <returns>The relative path or <paramref name="path"/> if the paths don't share the same root.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="relativeTo"/> or <paramref name="path"/> is <c>null</c> or an empty string.</exception>
    public static string GetRelativePath(string relativeTo, string path)
    {
        return GetRelativePath(relativeTo, path, StringComparison.CurrentCulture);
    }

    private static string GetRelativePath(string relativeTo, string path, StringComparison comparisonType)
    {
        ArgumentNullException.ThrowIfNull(relativeTo);
        ArgumentNullException.ThrowIfNull(path);

        if (IsEffectivelyEmpty(relativeTo.AsSpan()))
            throw new ArgumentException(nameof(relativeTo));
        if (IsEffectivelyEmpty(path.AsSpan()))
            throw new ArgumentException(nameof(path));

        relativeTo = GetFullPath(relativeTo);
        path = GetFullPath(path);

        // Need to check if the roots are different- if they are we need to return the "to" path.
        if (!AreRootsEqual(relativeTo, path, comparisonType))
            return path;

        int commonLength = GetCommonPathLength(relativeTo, path,
            ignoreCase: comparisonType == StringComparison.OrdinalIgnoreCase);

        // If there is nothing in common they can't share the same root, return the "to" path as is.
        if (commonLength == 0)
            return path;

        // Trailing separators aren't significant for comparison
        int relativeToLength = relativeTo.Length;
        if (EndsInDirectorySeparator(relativeTo.AsSpan()))
            relativeToLength--;

        bool pathEndsInSeparator = EndsInDirectorySeparator(path.AsSpan());
        int pathLength = path.Length;
        if (pathEndsInSeparator)
            pathLength--;

        // If we have effectively the same path, return "."
        if (relativeToLength == pathLength && commonLength >= relativeToLength) return ".";

        // We have the same root, we need to calculate the difference now using the
        // common Length and Segment count past the length.
        //
        // Some examples:
        //
        //  C:\Foo C:\Bar L3, S1 -> ..\Bar
        //  C:\Foo C:\Foo\Bar L6, S0 -> Bar
        //  C:\Foo\Bar C:\Bar\Bar L3, S2 -> ..\..\Bar\Bar
        //  C:\Foo\Foo C:\Foo\Bar L7, S1 -> ..\Bar

        var sb = new StringBuilder();
        sb.EnsureCapacity(Math.Max(relativeTo.Length, path.Length));

        // Add parent segments for segments past the common on the "from" path
        if (commonLength < relativeToLength)
        {
            sb.Append("..");

            for (int i = commonLength + 1; i < relativeToLength; i++)
            {
                if (IsDirectorySeparator(relativeTo[i]))
                {
                    sb.Append('\\');
                    sb.Append("..");
                }
            }
        }
        else if (IsDirectorySeparator(path[commonLength]))
        {
            // No parent segments and we need to eat the initial separator
            //  (C:\Foo C:\Foo\Bar case)
            commonLength++;
        }

        // Now add the rest of the "to" path, adding back the trailing separator
        int differenceLength = pathLength - commonLength;
        if (pathEndsInSeparator)
            differenceLength++;

        if (differenceLength > 0)
        {
            if (sb.Length > 0)
            {
                sb.Append('\\');
            }

            sb.Append(path.AsSpan(commonLength, differenceLength));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns true if the path ends in a directory separator.
    /// </summary>
    internal static bool EndsInDirectorySeparator([NotNullWhen(true)] string? path) =>
        !string.IsNullOrEmpty(path) && IsDirectorySeparator(path[^1]);

    /// <summary>
    /// Trims one trailing directory separator beyond the root of the path.
    /// </summary>
    internal static ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) =>
        EndsInDirectorySeparator(path) && !IsRoot(path) ? path.Slice(0, path.Length - 1) : path;

    /// <summary>
    /// Returns true if the path ends in a directory separator.
    /// </summary>
    internal static bool EndsInDirectorySeparator(ReadOnlySpan<char> path) =>
        path.Length > 0 && IsDirectorySeparator(path[^1]);

    internal static bool IsRoot(ReadOnlySpan<char> path)
        => path.Length == GetRootLength(path);


    // Expands the given path to a fully qualified path.
    public static string GetFullPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (path.Contains('\0'))
            throw new ArgumentException(nameof(path));

        return GetFullPathInternal(path);
    }

    public static string GetFullPath(string path, string basePath)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(basePath);

        if (!IsPathFullyQualified(basePath))
            throw new ArgumentException(nameof(basePath));

        if (basePath.Contains('\0') || path.Contains('\0'))
            throw new ArgumentException("null char in path");

        if (IsPathFullyQualified(path))
            return GetFullPathInternal(path);

        return GetFullPathInternal(CombineInternal(basePath, path));
    }

    // Gets the full path without argument validation
    private static string GetFullPathInternal(string path)
    {
        // Expand with current directory if necessary
        if (!IsPathRooted(path))
        {
            path = Combine(WBUtil.GetExecutablePath().ToBndPath(), path);
        }

        // We would ideally use realpath to do this, but it resolves symlinks and requires that the file actually exist.
        string collapsedString = RemoveRelativeSegments(path, GetRootLength(path));

        string result = collapsedString.Length == 0 ? "\\" : collapsedString;

        return result;
    }

    public static bool IsPathFullyQualified(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return IsPathFullyQualified(path.AsSpan());
    }

    public static bool IsPathFullyQualified(ReadOnlySpan<char> path)
    {
        return !IsPartiallyQualified(path);
    }

    internal static bool IsPartiallyQualified(ReadOnlySpan<char> path)
    {
        // This is much simpler than Windows where paths can be rooted, but not fully qualified (such as Drive Relative)
        // As long as the path is rooted in Unix it doesn't use the current directory and therefore is fully qualified.
        return !IsPathRooted(path);
    }


    /// <summary>
    /// Try to remove relative segments from the given path (without combining with a root).
    /// </summary>
    /// <param name="path">Input path</param>
    /// <param name="rootLength">The length of the root of the given path</param>
    internal static string RemoveRelativeSegments(string path, int rootLength)
    {
        var sb = new StringBuilder();

        if (RemoveRelativeSegments(path.AsSpan(), rootLength, ref sb))
        {
            path = sb.ToString();
        }

        return path;
    }

    /// <summary>
    /// Try to remove relative segments from the given path (without combining with a root).
    /// </summary>
    /// <param name="path">Input path</param>
    /// <param name="rootLength">The length of the root of the given path</param>
    /// <param name="sb">String builder that will store the result</param>
    /// <returns>"true" if the path was modified</returns>
    internal static bool RemoveRelativeSegments(ReadOnlySpan<char> path, int rootLength, ref StringBuilder sb)
    {
        bool flippedSeparator = false;

        int skip = rootLength;
        // We treat "\.." , "\." and "\\" as a relative segment. We want to collapse the first separator past the root presuming
        // the root actually ends in a separator. Otherwise the first segment for RemoveRelativeSegments
        // in cases like "\\?\C:\.\" and "\\?\C:\..\", the first segment after the root will be ".\" and "..\" which is not considered as a relative segment and hence not be removed.
        if (IsDirectorySeparator(path[skip - 1]))
            skip--;

        // Remove "//", "/./", and "/../" from the path by copying each character to the output,
        // except the ones we're removing, such that the builder contains the normalized path
        // at the end.
        if (skip > 0)
        {
            sb.Append(path.Slice(0, skip));
        }

        for (int i = skip; i < path.Length; i++)
        {
            char c = path[i];

            if (IsDirectorySeparator(c) && i + 1 < path.Length)
            {
                // Skip this character if it's a directory separator and if the next character is, too,
                // e.g. "parent//child" => "parent/child"
                if (IsDirectorySeparator(path[i + 1]))
                {
                    continue;
                }

                // Skip this character and the next if it's referring to the current directory,
                // e.g. "parent/./child" => "parent/child"
                if ((i + 2 == path.Length || IsDirectorySeparator(path[i + 2])) &&
                    path[i + 1] == '.')
                {
                    i++;
                    continue;
                }

                // Skip this character and the next two if it's referring to the parent directory,
                // e.g. "parent/child/../grandchild" => "parent/grandchild"
                if (i + 2 < path.Length &&
                    (i + 3 == path.Length || IsDirectorySeparator(path[i + 3])) &&
                    path[i + 1] == '.' && path[i + 2] == '.')
                {
                    // Unwind back to the last slash (and if there isn't one, clear out everything).
                    int s;
                    for (s = sb.Length - 1; s >= skip; s--)
                    {
                        if (IsDirectorySeparator(sb[s]))
                        {
                            sb.Length = (i + 3 >= path.Length && s == skip)
                                ? s + 1
                                : s; // to avoid removing the complete "\tmp\" segment in cases like \\?\C:\tmp\..\, C:\tmp\..
                            break;
                        }
                    }

                    if (s < skip)
                    {
                        sb.Length = skip;
                    }

                    i += 2;
                    continue;
                }
            }

            sb.Append(c);
        }

        // If we haven't changed the source path, return the original
        if (!flippedSeparator && sb.Length == path.Length)
        {
            return false;
        }

        // We may have eaten the trailing separator from the root when we started and not replaced it
        if (skip != rootLength && sb.Length < rootLength)
        {
            sb.Append(path[rootLength - 1]);
        }

        return true;
    }

    /// <summary>
    /// Returns true if the two paths have the same root
    /// </summary>
    internal static bool AreRootsEqual(string? first, string? second, StringComparison comparisonType)
    {
        int firstRootLength = GetRootLength(first.AsSpan());
        int secondRootLength = GetRootLength(second.AsSpan());

        return firstRootLength == secondRootLength
               && string.Compare(
                   strA: first,
                   indexA: 0,
                   strB: second,
                   indexB: 0,
                   length: firstRootLength,
                   comparisonType: comparisonType) == 0;
    }

    private static string CombineInternal(string first, string second)
    {
        if (string.IsNullOrEmpty(first))
            return second;

        if (string.IsNullOrEmpty(second))
            return first;

        if (IsPathRooted(second.AsSpan()))
            return second;

        return JoinInternal(first.AsSpan(), second.AsSpan());
    }

    private static string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        bool hasSeparator = IsDirectorySeparator(first[^1]) || IsDirectorySeparator(second[0]);

        return hasSeparator ? string.Concat(first, second) : string.Concat(first, "\\", second);
    }

    public static string GetFileNameWithoutAnyExtensions(string path)
    {
        return GetFileName(path).Split(".").First();
    }

    public static string GetFullExtensions(string path)
    {
        var split = GetFileName(path).Split(".");
        if (split.Length > 1)
            return "." + string.Join(".", GetFileName(path).Split(".").Skip(1));
        return "";
    }

    public static void WriteSanitizedBinderFilePath(this XElement element, string path, string pathElementName = "path")
    {
        string dir = Path.GetDirectoryName(path) ?? "";
        string filename = Path.GetFileName(path);
        string sanitized = OSPath.SanitizeFilename(path);

        if (filename == sanitized)
        {
            element.Add(new XElement(pathElementName, path));
        }
        else
        {
            element.Add(new XElement("in" + pathElementName.FirstCharToUpper(), path));
            element.Add(new XElement("out" + pathElementName.FirstCharToUpper(), Path.Combine(dir, sanitized)));
        }
    }

    public static string GetSanitizedBinderFilePath(this XElement element, string pathElementName = "path",
        bool outName = false)
    {
        if (element.Element(pathElementName) != null)
            return element.Element(pathElementName)!.Value;
        var otherName =
            outName ? "out" + pathElementName.FirstCharToUpper() : "in" + pathElementName.FirstCharToUpper();
        if (element.Element(otherName) != null)
            return element.Element(otherName)!.Value;

        throw new InvalidDataException("File element is missing path.");
    }
}

public static class OSPath
{
    public static string Combine(params string[] paths)
    {
        return Path.Combine(paths);
    }

    /// <summary>
    /// Returns the name and extension parts of the given path. The resulting string contains
    /// the characters of path that follow the last separator in path. The resulting string is
    /// null if path is null.
    /// </summary>
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileName(string? path)
    {
        return Path.GetFileName(path);
    }

    /// <summary>
    /// The returned ReadOnlySpan contains the characters of the path that follows the last separator in path.
    /// </summary>
    public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
    {
        return Path.GetFileName(path);
    }

    /// <summary>
    /// Returns the directory portion of a file path. This method effectively
    /// removes the last segment of the given file path, i.e. it returns a
    /// string consisting of all characters up to but not including the last
    /// backslash ("\") in the file path. The returned value is null if the
    /// specified path is null, empty, or a root (such as "\", "C:", or
    /// "\\server\share").
    /// </summary>
    /// <remarks>
    /// Directory separators are normalized in the returned string.
    /// </remarks>
    public static string? GetDirectoryName(string? path)
    {
        return Path.GetDirectoryName(path);
    }

    /// <summary>
    /// Returns the directory portion of a file path. The returned value is empty
    /// if the specified path is null, empty, or a root (such as "\", "C:", or
    /// "\\server\share").
    /// </summary>
    /// <remarks>
    /// Unlike the string overload, this method will not normalize directory separators.
    /// </remarks>
    public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path)
    {
        return Path.GetDirectoryName(path);
    }

    // Expands the given path to a fully qualified path.
    public static string GetFullPath(string path)
    {
        return Path.GetFullPath(path);
    }

    public static string GetFullPath(string path, string basePath)
    {
        return Path.GetFullPath(path, basePath);
    }

    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileNameWithoutExtension(string? path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }

    /// <summary>
    /// Returns the characters between the last separator and last (.) in the path.
    /// </summary>
    public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }

    /// <summary>
    /// Returns the extension of the given path. The returned value includes the period (".") character of the
    /// extension except when you have a terminal period when you get string.Empty, such as ".exe" or ".cpp".
    /// The returned value is null if the given path is null or empty if the given path does not include an
    /// extension.
    /// </summary>
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetExtension(string? path)
    {
        return Path.GetExtension(path);
    }

    /// <summary>
    /// Returns the extension of the given path.
    /// </summary>
    /// <remarks>
    /// The returned value is an empty ReadOnlySpan if the given path does not include an extension.
    /// </remarks>
    public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
    {
        return Path.GetExtension(path);
    }

    // Changes the extension of a file path. The path parameter
    // specifies a file path, and the extension parameter
    // specifies a file extension (with a leading period, such as
    // ".exe" or ".cs").
    //
    // The function returns a file path with the same root, directory, and base
    // name parts as path, but with the file extension changed to
    // the specified extension. If path is null, the function
    // returns null. If path does not contain a file extension,
    // the new file extension is appended to the path. If extension
    // is null, any existing extension is removed from path.
    [return: NotNullIfNotNull(nameof(path))]
    public static string? ChangeExtension(string? path, string? extension)
    {
        return Path.ChangeExtension(path, extension);
    }


    /// <summary>
    /// Create a relative path from one path to another. Paths will be resolved before calculating the difference.
    /// Default path comparison for the active platform will be used (OrdinalIgnoreCase for Windows or Mac, Ordinal for Unix).
    /// </summary>
    /// <param name="relativeTo">The source path the output should be relative to. This path is always considered to be a directory.</param>
    /// <param name="path">The destination path.</param>
    /// <returns>The relative path or <paramref name="path"/> if the paths don't share the same root.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="relativeTo"/> or <paramref name="path"/> is <c>null</c> or an empty string.</exception>
    public static string GetRelativePath(string relativeTo, string path)
    {
        return Path.GetRelativePath(relativeTo, path);
    }

    public static string GetFileNameWithoutAnyExtensions(string path)
    {
        return GetFileName(path).Split(".").First();
    }

    public static string GetFullExtensions(string path)
    {
        var split = GetFileName(path).Split(".");
        if (split.Length > 1)
            return "." + string.Join(".", GetFileName(path).Split(".").Skip(1));
        return "";
    }

    public static string SanitizeFilename(string path)
    {
        return Path.GetInvalidFileNameChars()
            .Aggregate(path, (current, c) => current.Replace(c, '_'));
    }

    public static List<string> ProcessPathGlobs(List<string> paths)
    {
        var processedPaths = new List<string>();
        foreach (string path in paths.Select(p => {
                     try
                     {
                         return Path.GetFullPath(p);
                     }
                     catch (Exception e)
                     {
                         Console.WriteLine($"Invalid path: {p} ({e.Message})");
                         return null;
                     }
                 }).Where(p => p != null).Cast<string>()) {
            if (path.Contains('*'))
            {
                var matcher = new Matcher();
                var rootParts = path.Split(Path.DirectorySeparatorChar).TakeWhile(part => !part.Contains('*')).ToList();
                var root = string.Join(Path.DirectorySeparatorChar, rootParts);
                var rest = path.Substring(root.Length + 1);

                matcher = matcher.AddInclude(rest.Replace(Path.DirectorySeparatorChar, '/'));

                var rootPath = Path.Combine(Environment.CurrentDirectory, root);
                if (!Directory.Exists(rootPath))
                {
                    Console.Error.WriteLine($"Invalid path: {rootPath}");
                    continue;
                }
                var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, root), "*",
                    SearchOption.AllDirectories);
                var fileMatch = matcher.Match(Path.Combine(Environment.CurrentDirectory, root), files);
                if (fileMatch.HasMatches)
                {
                    processedPaths.AddRange(fileMatch.Files.Select(m => Path.Combine(root, m.Path)).Where(globFilter).ToList());
                }

                var dirs = Directory.GetDirectories(Path.Combine(Environment.CurrentDirectory, root), "*",
                    SearchOption.AllDirectories);
                var dirMatch = matcher.Match(Path.Combine(Environment.CurrentDirectory, root), dirs);
                if (dirMatch.HasMatches)
                {
                    processedPaths.AddRange(dirMatch.Files.Select(m => Path.Combine(root, m.Path)).Where(globFilter).ToList());
                }
            }
            else
            {
                processedPaths.Add(path);
            }
        }

        return processedPaths.Select(path => Path.GetFullPath(path)).ToList();

        bool globFilter(string path)
        {
            return !path.EndsWith(".bak");
        }
    }
}