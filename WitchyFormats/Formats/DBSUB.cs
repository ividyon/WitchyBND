using System.Collections.Generic;
using SoulsFormats;

namespace WitchyFormats
{
    /// <summary>
    /// Debriefing video subtitle files for Armored Core 4th generation games.
    /// </summary>
    public class DBSUB : SoulsFile<DBSUB>
    {
        /// <summary>
        /// The subtitle entries of this DBSUB.
        /// </summary>
        public List<SubtitleEntry> SubtitleEntries { get; set; } = new List<SubtitleEntry>();

        /// <summary>
        /// The video entries of this DBSUB.
        /// </summary>
        public List<VideoEntry> VideoEntries { get; set; } = new List<VideoEntry>();

        /// <summary>
        /// Unknown; Believed to be EventID for voice call videos.
        /// </summary>
        public uint EventID { get; set; }

        /// <summary>
        /// Whether or not strings should be read and written as Unicode.
        /// </summary>
        /// <remarks>
        /// Older test subtitle files are in ShiftJIS and not Unicode.
        /// </remarks>
        public bool Unicode { get; set; } = true;

        /// <summary>
        /// The size of the header.
        /// </summary>
        private const byte HEADER_SIZE = 0x20;

        /// <summary>
        /// The size of subtitle entries.
        /// </summary>
        private const byte SUBTITLE_ENTRY_SIZE = 0x10;

        /// <summary>
        /// The size of video entries.
        /// </summary>
        private const byte VIDEO_ENTRY_SIZE = 0x20;

        /// <summary>
        /// Returns true if the data appears to be a Debriefing Subtitle container.
        /// </summary>
        protected override bool Is(BinaryReaderEx br)
        {
            br.BigEndian = true;
            if (br.Length < HEADER_SIZE)
                return false;

            // Get necessary info for checks.
            uint dataOffset = br.ReadUInt32();
            uint subtitleCount = br.ReadUInt32();
            uint subtitleEntriesOffset = br.ReadUInt32();
            uint videoCount = br.ReadUInt32();
            uint videoEntriesOffset = br.ReadUInt32();
            br.Skip(4); // Skip event ID
            uint unk18 = br.ReadUInt32();
            uint unk1C = br.ReadUInt32();

            // These are always null.
            if (unk18 != 0 || unk1C != 0)
            {
                return false;
            }

            // Offsets must not be less than the header in size.
            if (dataOffset < HEADER_SIZE || subtitleEntriesOffset < HEADER_SIZE || videoEntriesOffset < HEADER_SIZE)
            {
                return false;
            }

            // If stream length is the size of the header, counts will be 0, offsets will be the size of the header.
            if (br.Length == HEADER_SIZE && ((subtitleCount != 0 || videoCount != 0) || (subtitleEntriesOffset != HEADER_SIZE || videoEntriesOffset != HEADER_SIZE)))
            {
                return false;
            }

            // There should be enough of the stream left to even read the entries.
            long totalHeaderSize = HEADER_SIZE + (subtitleCount * SUBTITLE_ENTRY_SIZE) + (videoCount * VIDEO_ENTRY_SIZE);
            if (totalHeaderSize > br.Length)
            {
                return false;
            }

            // Subtitle entries come before Video entries.
            if (subtitleEntriesOffset > videoEntriesOffset)
            {
                return false;
            }

            // Video entries come before the data.
            if (videoEntriesOffset > dataOffset)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Deserializes file data from a stream.
        /// </summary>
        protected override void Read(BinaryReaderEx br)
        {
            // Read Header
            br.BigEndian = true;
            uint dataOffset = br.ReadUInt32();
            uint subtitleCount = br.ReadUInt32();
            uint subtitleEntriesOffset = br.ReadUInt32();
            uint videoCount = br.ReadUInt32();
            uint videoEntriesOffset = br.ReadUInt32();
            EventID = br.ReadUInt32();
            br.AssertUInt32(0);
            br.AssertUInt32(0);

            // Retrieve subtitle entries
            if (subtitleCount > 0)
            {
                br.Position = subtitleEntriesOffset;
                SubtitleEntries = new List<SubtitleEntry>();
                for (int i = 0; i < subtitleCount; i++)
                {
                    // Read entry
                    short frameDelay = br.ReadInt16();
                    short frameTime = br.ReadInt16();
                    uint textOffset = br.ReadUInt32();
                    br.AssertUInt32(0);
                    br.AssertUInt32(0);

                    // Add entry
                    var subtitle = new SubtitleEntry(GetString(br, dataOffset + textOffset), frameDelay, frameTime);
                    SubtitleEntries.Add(subtitle);
                }
            }

            // Retrieve video entries
            if (videoCount > 0)
            {
                br.Position = videoEntriesOffset;
                for (int i = 0; i < videoCount; i++)
                {
                    // Read entry
                    uint nameOffset = br.ReadUInt32();
                    br.AssertUInt32(0);
                    short unk08 = br.ReadInt16();
                    short unk0A = br.ReadInt16();
                    short width = br.ReadInt16();
                    short height = br.ReadInt16();
                    br.AssertUInt32(0);
                    br.AssertUInt32(0);
                    br.AssertUInt32(0);
                    br.AssertUInt32(0);

                    // Add entry
                    VideoEntries.Add(new VideoEntry(GetString(br, dataOffset + nameOffset), width, height, unk08, unk0A));
                }
            }
        }

        /// <summary>
        /// Serializes file data to a stream.
        /// </summary>
        protected override void Write(BinaryWriterEx bw)
        {
            // Write Header
            bw.BigEndian = true;
            bw.ReserveUInt32("dataOffset");
            bw.WriteUInt32((uint)SubtitleEntries.Count);
            bw.ReserveUInt32("subtitleEntriesOffset");
            bw.WriteUInt32((uint)VideoEntries.Count);
            bw.ReserveUInt32("videoEntriesOffset");
            bw.WriteUInt32(EventID);
            bw.WriteUInt32(0);
            bw.WriteUInt32(0);

            // Write Subtitle Entries
            bw.FillUInt32("subtitleEntriesOffset", (uint)bw.Position);
            for (int i = 0; i < SubtitleEntries.Count; i++)
            {
                SubtitleEntry subtitleEntry = SubtitleEntries[i];
                bw.WriteInt16(subtitleEntry.FrameDelay);
                bw.WriteInt16(subtitleEntry.FrameTime);
                bw.ReserveUInt32($"subtitleTextOffset_{i}");
                bw.WriteUInt32(0);
                bw.WriteUInt32(0);
            }

            // Write Video Entries
            bw.FillUInt32("videoEntriesOffset", (uint)bw.Position);
            for (int i = 0; i < VideoEntries.Count; i++)
            {
                VideoEntry videoEntry = VideoEntries[i];
                bw.ReserveUInt32($"videoNameOffset_{i}");
                bw.WriteUInt32(0);
                bw.WriteInt16(videoEntry.Unk08);
                bw.WriteInt16(videoEntry.Unk0A);
                bw.WriteInt16(videoEntry.Width);
                bw.WriteInt16(videoEntry.Height);
                bw.WriteUInt32(0);
                bw.WriteUInt32(0);
                bw.WriteUInt32(0);
                bw.WriteUInt32(0);
            }

            // Fill Data Offset
            uint dataOffset = (uint)bw.Position;
            bw.FillUInt32("dataOffset", dataOffset);

            // Write Subtitle Text
            for (int i = 0; i < SubtitleEntries.Count; i++)
            {
                bw.FillUInt32($"subtitleTextOffset_{i}", (uint)bw.Position - dataOffset);

                if (Unicode)
                {
                    bw.WriteUTF16(SubtitleEntries[i].Text ?? string.Empty, true);
                    bw.WriteByte(0);
                    bw.WriteByte(0);
                }
                else
                {
                    bw.WriteShiftJIS(SubtitleEntries[i].Text ?? string.Empty, true);
                }
            }

            // Write Video Names
            for (int i = 0; i < VideoEntries.Count; i++)
            {
                bw.FillUInt32($"videoNameOffset_{i}", (uint)bw.Position - dataOffset);

                if (Unicode)
                {
                    bw.WriteUTF16(VideoEntries[i].Name ?? string.Empty, true);
                    bw.WriteByte(0);
                    bw.WriteByte(0);
                }
                else
                {
                    bw.WriteShiftJIS(VideoEntries[i].Name ?? string.Empty, true);
                }
            }
        }

        /// <summary>
        /// Get strings while attempting to read as ShiftJIS if failing to do so.
        /// </summary>
        /// <param name="br">The stream.</param>
        /// <param name="offset">The offset the string is at.</param>
        /// <returns>The read string.</returns>
        private string GetString(BinaryReaderEx br, long offset)
        {
            long pos = br.Position;
            
            string str;
            try
            {
                br.Position = offset;

                if (Unicode)
                {
                    str = br.ReadUTF16();
                    br.AssertUInt16(0);
                }
                else
                {
                    str = br.ReadShiftJIS();
                }
            }
            catch
            {
                br.Position = offset;

                // We had an error so flip detection
                Unicode = !Unicode;
                if (Unicode)
                {
                    str = br.ReadUTF16();
                    br.AssertUInt16(0);
                }
                else
                {
                    str = br.ReadShiftJIS();
                }
            }
            br.Position = pos;
            return str;
        }

        #region Sub Classes

        /// <summary>
        /// A subtitle entry.
        /// </summary>
        public class SubtitleEntry
        {
            /// <summary>
            /// The text in the subtitle.
            /// </summary>
            public string Text { get; set; } = string.Empty;

            /// <summary>
            /// How many frames from the last subtitle or start until this subtitle shows.
            /// </summary>
            public short FrameDelay { get; set; }

            /// <summary>
            /// How many frames the subtitle lasts for.
            /// </summary>
            public short FrameTime { get; set; }

            /// <summary>
            /// Creates a new Subtitle Entry.
            /// </summary>
            public SubtitleEntry(string text, short frameDelay, short frameTime)
            {
                Text = text;
                FrameDelay = frameDelay;
                FrameTime = frameTime;
            }

            /// <summary>
            /// Creates a new Subtitle Entry without text.
            /// </summary>
            public SubtitleEntry(short frameDelay, short frameTime)
            {
                FrameDelay = frameDelay;
                FrameTime = frameTime;
            }

            /// <summary>
            /// Creates a new, default subtitle entry.
            /// </summary>
            public SubtitleEntry()
            {
                Text = string.Empty;
                FrameDelay = 10;
                FrameTime = 0;
            }
        }

        /// <summary>
        /// A video entry.
        /// </summary>
        public class VideoEntry
        {
            /// <summary>
            /// The file name of the video, not including extension.
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// The width of the video in pixels.
            /// </summary>
            public short Width { get; set; } = 0;

            /// <summary>
            /// The height of the video in pixels.
            /// </summary>
            public short Height { get; set; } = 0;

            /// <summary>
            /// Unknown; Only appears in test files.
            /// </summary>
            public short Unk08 { get; set; } = 0;

            /// <summary>
            /// Unknown; Only appears in test files.
            /// </summary>
            public short Unk0A { get; set; } = 0;

            /// <summary>
            /// Creates a new Video Entry with full data.
            /// </summary>
            public VideoEntry(string name, short width, short height, short unk08, short unk0A)
            {
                Name = name;
                Width = width;
                Height = height;
                Unk08 = unk08;
                Unk0A = unk0A;
            }

            /// <summary>
            /// Creates a new Video Entry.
            /// </summary>
            public VideoEntry(string name, short width, short height)
            {
                Name = name;
                Width = width;
                Height = height;
            }

            /// <summary>
            /// Creates a new Video Entry without a name.
            /// </summary>
            public VideoEntry(short width, short height)
            {
                Width = width;
                Height = height;
            }

            /// <summary>
            /// Creates a new VideoEntry with placeholder values.
            /// </summary>
            public VideoEntry(){}
        }

        #endregion
    }
}
