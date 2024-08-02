using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using SoulsFormats;
using WitchyLib;

namespace WitchyBND.Parsers;

public class WGPARAM : WXMLParser
{
    public override string Name => "GPARAM";
    public override bool Is(string path, byte[]? data, out ISoulsFile? file)
    {
      return IsRead<GPARAM>(path, data, out file);
    }

    public override bool? IsSimple(string path)
    {
      string filename = Path.GetFileName(path).ToLower();
      return filename.EndsWith(".gparam") || filename.EndsWith(".gparam.dcx");
    }

    public override void Unpack(string srcPath, ISoulsFile? file, bool recursive)
    {
      GPARAM gparam = (file as GPARAM)!;
      string destPath = GetUnpackDestPath(srcPath, recursive);
      if (File.Exists(destPath))
        WBUtil.Backup(destPath);
      using (XmlWriter xw = XmlWriter.Create(destPath, new XmlWriterSettings
             {
        Indent = true
      }))
      {
        xw.WriteStartElement(XmlTag);
        AddLocationToXml(srcPath, recursive, xw);

        xw.WriteElementString("compression", gparam.Compression.ToString());
        xw.WriteElementString("game", gparam.Game.ToString());
        xw.WriteElementString("unk0D", gparam.Unk0D.ToString());
        xw.WriteElementString("unk14", gparam.Unk14.ToString());
        xw.WriteElementString("unk40", gparam.Unk40.ToString());
        if (gparam.Game == GPARAM.GPGame.Sekiro)
          xw.WriteElementString("unk50", gparam.Unk50.ToString());
        xw.WriteStartElement("groups");
        foreach (GPARAM.Group group in gparam.Groups)
        {
          xw.WriteStartElement("group");
          xw.WriteAttributeString("name1", group.Name1);
          if (gparam.Game == GPARAM.GPGame.DarkSouls3 || gparam.Game == GPARAM.GPGame.Sekiro)
          {
            xw.WriteAttributeString("name2", group.Name2);
            xw.WriteStartElement("comments");
            foreach (string comment in group.Comments)
              xw.WriteElementString("comment", comment);
            xw.WriteEndElement();
          }
          foreach (GPARAM.Param obj in group.Params)
          {
            xw.WriteStartElement("param");
            xw.WriteAttributeString("name1", obj.Name1);
            if (gparam.Game == GPARAM.GPGame.DarkSouls3 || gparam.Game == GPARAM.GPGame.Sekiro)
              xw.WriteAttributeString("name2", obj.Name2);
            xw.WriteAttributeString("type", obj.Type.ToString());
            for (int index = 0; index < obj.Values.Count; ++index)
            {
              xw.WriteStartElement("value");
              xw.WriteAttributeString("id", obj.ValueIDs[index].ToString());
              if (gparam.Game == GPARAM.GPGame.Sekiro)
                xw.WriteAttributeString("timeOfDay", obj.TimeOfDay[index].ToString());
              if (obj.Type == GPARAM.ParamType.Float2)
              {
                Vector2 vector2 = (Vector2) obj.Values[index];
                XmlWriter xmlWriter2 = xw;
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
                interpolatedStringHandler.AppendFormatted(vector2.X);
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(vector2.Y);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                xmlWriter2.WriteString(stringAndClear);
              }
              else if (obj.Type == GPARAM.ParamType.Float3)
              {
                Vector3 vector3 = (Vector3) obj.Values[index];
                XmlWriter xmlWriter3 = xw;
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
                interpolatedStringHandler.AppendFormatted(vector3.X);
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(vector3.Y);
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(vector3.Z);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                xmlWriter3.WriteString(stringAndClear);
              }
              else if (obj.Type == GPARAM.ParamType.Float4)
              {
                Vector4 vector4 = (Vector4) obj.Values[index];
                XmlWriter xmlWriter4 = xw;
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 4);
                interpolatedStringHandler.AppendFormatted(vector4.X);
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(vector4.Y);
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(vector4.Z);
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(vector4.W);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                xmlWriter4.WriteString(stringAndClear);
              }
              else if (obj.Type == GPARAM.ParamType.Byte4)
              {
                byte[] numArray = (byte[]) obj.Values[index];
                XmlWriter xmlWriter5 = xw;
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 4);
                interpolatedStringHandler.AppendFormatted(numArray[0], "X2");
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(numArray[1], "X2");
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(numArray[2], "X2");
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(numArray[3], "X2");
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                xmlWriter5.WriteString(stringAndClear);
              }
              else
                xw.WriteString(obj.Values[index].ToString());
              xw.WriteEndElement();
            }
            xw.WriteEndElement();
          }
          xw.WriteEndElement();
        }
        xw.WriteEndElement();
        xw.WriteStartElement("unk3s");
        foreach (GPARAM.Unk3 unk3 in gparam.Unk3s)
        {
          xw.WriteStartElement("unk3");
          xw.WriteAttributeString("group_index", unk3.GroupIndex.ToString());
          if (gparam.Game == GPARAM.GPGame.Sekiro)
            xw.WriteAttributeString("unk0C", unk3.Unk0C.ToString());
          foreach (int valueId in unk3.ValueIDs)
            xw.WriteElementString("value_id", valueId.ToString());
          xw.WriteEndElement();
        }
        xw.WriteEndElement();
        xw.WriteElementString("unk_block_2", string.Join(" ", gparam.UnkBlock2.Select((Func<byte, string>) (b => b.ToString("X2")))));
        xw.WriteEndElement();
        xw.Close();
      }
    }

    public override void Repack(string srcPath, bool recursive)
    {
      GPARAM gparam = new GPARAM();
      XmlDocument xmlDocument = new XmlDocument();
      xmlDocument.Load(srcPath);
      DCX.Type result;
      Enum.TryParse(xmlDocument.SelectSingleNode("gparam/compression")?.InnerText ?? "None", out result);
      gparam.Compression = result;
      Enum.TryParse(xmlDocument.SelectSingleNode("gparam/game").InnerText, out gparam.Game);
      gparam.Unk0D = bool.Parse(xmlDocument.SelectSingleNode("gparam/unk0D").InnerText);
      gparam.Unk14 = int.Parse(xmlDocument.SelectSingleNode("gparam/unk14").InnerText);
      gparam.Unk40 = float.Parse(xmlDocument.SelectSingleNode("gparam/unk40").InnerText);
      if (gparam.Game == GPARAM.GPGame.Sekiro)
        gparam.Unk50 = float.Parse(xmlDocument.SelectSingleNode("gparam/unk50").InnerText);
      foreach (XmlNode selectNode1 in xmlDocument.SelectNodes("gparam/groups/group"))
      {
        string innerText1 = selectNode1.Attributes["name1"].InnerText;
        GPARAM.Group group;
        if (gparam.Game == GPARAM.GPGame.DarkSouls2)
        {
          group = new GPARAM.Group(innerText1, null);
        }
        else
        {
          string innerText2 = selectNode1.Attributes["name2"].InnerText;
          group = new GPARAM.Group(innerText1, innerText2);
          foreach (XmlNode selectNode2 in selectNode1.SelectNodes("comments/comment"))
            group.Comments.Add(selectNode2.InnerText);
        }
        foreach (XmlNode selectNode3 in selectNode1.SelectNodes("param"))
        {
          string innerText3 = selectNode3.Attributes["name1"].InnerText;
          GPARAM.ParamType type = (GPARAM.ParamType) Enum.Parse(typeof (GPARAM.ParamType), selectNode3.Attributes["type"].InnerText);
          GPARAM.Param obj;
          if (gparam.Game == GPARAM.GPGame.DarkSouls2)
          {
            obj = new GPARAM.Param(innerText3, null, type);
          }
          else
          {
            string innerText4 = selectNode3.Attributes["name2"].InnerText;
            obj = new GPARAM.Param(innerText3, innerText4, type);
            if (gparam.Game == GPARAM.GPGame.Sekiro)
              obj.TimeOfDay = new List<float>();
          }
          foreach (XmlNode selectNode4 in selectNode3.SelectNodes("value"))
          {
            obj.ValueIDs.Add(int.Parse(selectNode4.Attributes["id"].InnerText));
            if (gparam.Game == GPARAM.GPGame.Sekiro)
              obj.TimeOfDay.Add(float.Parse(selectNode4.Attributes["timeOfDay"].InnerText));
            switch (obj.Type)
            {
              case GPARAM.ParamType.Byte:
                obj.Values.Add(byte.Parse(selectNode4.InnerText));
                continue;
              case GPARAM.ParamType.Short:
                obj.Values.Add(short.Parse(selectNode4.InnerText));
                continue;
              case GPARAM.ParamType.IntA:
                obj.Values.Add(int.Parse(selectNode4.InnerText));
                continue;
              case GPARAM.ParamType.BoolA:
                obj.Values.Add(bool.Parse(selectNode4.InnerText));
                continue;
              case GPARAM.ParamType.IntB:
                obj.Values.Add(int.Parse(selectNode4.InnerText));
                continue;
              case GPARAM.ParamType.Float:
                obj.Values.Add(float.Parse(selectNode4.InnerText));
                continue;
              case GPARAM.ParamType.BoolB:
                obj.Values.Add(bool.Parse(selectNode4.InnerText));
                continue;
              case GPARAM.ParamType.Float2:
                float[] array1 = selectNode4.InnerText.Split(' ').Select((Func<string, float>) (f => float.Parse(f))).ToArray();
                obj.Values.Add(new Vector2(array1[0], array1[1]));
                continue;
              case GPARAM.ParamType.Float3:
                float[] array2 = selectNode4.InnerText.Split(' ').Select((Func<string, float>) (f => float.Parse(f))).ToArray();
                obj.Values.Add(new Vector3(array2[0], array2[1], array2[1]));
                continue;
              case GPARAM.ParamType.Float4:
                float[] array3 = selectNode4.InnerText.Split(' ').Select((Func<string, float>) (f => float.Parse(f))).ToArray();
                obj.Values.Add(new Vector4(array3[0], array3[1], array3[2], array3[3]));
                continue;
              case GPARAM.ParamType.Byte4:
                byte[] array4 = selectNode4.InnerText.Split(' ').Select((Func<string, byte>) (b => byte.Parse(b, NumberStyles.AllowHexSpecifier))).ToArray();
                obj.Values.Add(array4);
                continue;
              default:
                continue;
            }
          }
          group.Params.Add(obj);
        }
        gparam.Groups.Add(group);
      }
      foreach (XmlNode selectNode5 in xmlDocument.SelectNodes("gparam/unk3s/unk3"))
      {
        GPARAM.Unk3 unk3 = new GPARAM.Unk3(int.Parse(selectNode5.Attributes["group_index"].InnerText));
        if (gparam.Game == GPARAM.GPGame.Sekiro)
          unk3.Unk0C = int.Parse(selectNode5.Attributes["unk0C"].InnerText);
        foreach (XmlNode selectNode6 in selectNode5.SelectNodes("value_id"))
          unk3.ValueIDs.Add(int.Parse(selectNode6.InnerText));
        gparam.Unk3s.Add(unk3);
      }
      gparam.UnkBlock2 = xmlDocument.SelectSingleNode("gparam/unk_block_2").InnerText.Split(new char[1]
      {
        ' '
      }, StringSplitOptions.RemoveEmptyEntries).Select((Func<string, byte>) (s => Convert.ToByte(s, 16))).ToArray();

      XElement xml = LoadXml(srcPath);
      string path = GetRepackDestPath(srcPath, xml);
      if (File.Exists(path))
        WBUtil.Backup(path);
      gparam.TryWriteSoulsFile(path);
    }
}