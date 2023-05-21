using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using SoulsFormats;

namespace WitchyFormats;

// Source: YabberAvocado zip in ?ServerName?
//
// Decompiled with JetBrains decompiler
// Type: SoulsFormats.GPARAM
// Assembly: SoulsFormats, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 91F4CB12-0CD3-441D-AE68-51FE09D1AE40
// Assembly location: SoulsFormats.dll
public class GPARAM : SoulsFile<GPARAM>
{
    public GPGame Game;
    public bool Unk0D;
    public int Unk14;
    public float Unk40;
    public float Unk50;
    public List<Group> Groups;
    public byte[] UnkBlock2;
    public List<Unk3> Unk3s;

    public GPARAM()
    {
        Game = GPGame.Sekiro;
        Groups = new List<Group>();
        UnkBlock2 = new byte[0];
        Unk3s = new List<Unk3>();
    }

    protected override bool Is(BinaryReaderEx br)
    {
        if (br.Length < 4L)
            return false;
        string ascii = br.GetASCII(0L, 4);
        return ascii == "filt" || ascii == "f\0i\0";
    }

    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = false;
        if (br.AssertASCII("filt", "f\0i\0") == "f\0i\0")
            br.AssertASCII("l\0t\0");
        Game = br.ReadEnum32<GPGame>();
        int num1 = br.AssertByte(new byte[1]);
        Unk0D = br.ReadBoolean();
        int num2 = br.AssertInt16(new short[1]);
        int num3 = br.ReadInt32();
        Unk14 = br.ReadInt32();
        br.AssertInt32(64, 80, 84);
        Offsets offsets = new Offsets();
        offsets.GroupHeaders = br.ReadInt32();
        offsets.ParamHeaderOffsets = br.ReadInt32();
        offsets.ParamHeaders = br.ReadInt32();
        offsets.Values = br.ReadInt32();
        offsets.ValueIDs = br.ReadInt32();
        offsets.Unk2 = br.ReadInt32();
        int capacity = br.ReadInt32();
        offsets.Unk3 = br.ReadInt32();
        offsets.Unk3ValueIDs = br.ReadInt32();
        Unk40 = br.ReadSingle();
        if (Game == GPGame.DarkSouls3 || Game == GPGame.Sekiro)
        {
            offsets.CommentOffsetsOffsets = br.ReadInt32();
            offsets.CommentOffsets = br.ReadInt32();
            offsets.Comments = br.ReadInt32();
        }

        if (Game == GPGame.Sekiro)
            Unk50 = br.ReadSingle();
        Groups = new List<Group>(num3);
        for (int index = 0; index < num3; ++index)
            Groups.Add(new Group(br, Game, index, offsets));
        UnkBlock2 = br.GetBytes(offsets.Unk2, offsets.Unk3 - offsets.Unk2);
        br.Position = offsets.Unk3;
        Unk3s = new List<Unk3>(capacity);
        for (int index = 0; index < capacity; ++index)
            Unk3s.Add(new Unk3(br, Game, offsets));
        if (Game != GPGame.DarkSouls3 && Game != GPGame.Sekiro)
            return;
        int[] int32s = br.GetInt32s(offsets.CommentOffsetsOffsets, num3);
        int num4 = offsets.Comments - offsets.CommentOffsets;
        for (int index1 = 0; index1 < num3; ++index1)
        {
            int num5 = index1 != num3 - 1 ? (int32s[index1 + 1] - int32s[index1]) / 4 : (num4 - int32s[index1]) / 4;
            br.Position = offsets.CommentOffsets + int32s[index1];
            for (int index2 = 0; index2 < num5; ++index2)
            {
                int num6 = br.ReadInt32();
                string utF16 = br.GetUTF16(offsets.Comments + num6);
                Groups[index1].Comments.Add(utF16);
            }
        }
    }

    protected override void Write(BinaryWriterEx bw)
    {
        bw.BigEndian = false;
        if (Game == GPGame.DarkSouls2)
            bw.WriteASCII("filt");
        else
            bw.WriteUTF16("filt");
        bw.WriteUInt32((uint)Game);
        bw.WriteByte(0);
        bw.WriteBoolean(Unk0D);
        bw.WriteInt16(0);
        bw.WriteInt32(Groups.Count);
        bw.WriteInt32(Unk14);
        bw.ReserveInt32("HeaderSize");
        bw.ReserveInt32("GroupHeadersOffset");
        bw.ReserveInt32("ParamHeaderOffsetsOffset");
        bw.ReserveInt32("ParamHeadersOffset");
        bw.ReserveInt32("ValuesOffset");
        bw.ReserveInt32("ValueIDsOffset");
        bw.ReserveInt32("UnkOffset2");
        bw.WriteInt32(Unk3s.Count);
        bw.ReserveInt32("UnkOffset3");
        bw.ReserveInt32("Unk3ValuesOffset");
        bw.WriteSingle(Unk40);
        if (Game == GPGame.DarkSouls3 || Game == GPGame.Sekiro)
        {
            bw.ReserveInt32("CommentOffsetsOffsetsOffset");
            bw.ReserveInt32("CommentOffsetsOffset");
            bw.ReserveInt32("CommentsOffset");
        }

        if (Game == GPGame.Sekiro)
            bw.WriteSingle(Unk50);
        bw.FillInt32("HeaderSize", (int)bw.Position);
        for (int index = 0; index < Groups.Count; ++index)
            Groups[index].WriteHeaderOffset(bw, index);
        int position1 = (int)bw.Position;
        bw.FillInt32("GroupHeadersOffset", position1);
        for (int index = 0; index < Groups.Count; ++index)
            Groups[index].WriteHeader(bw, Game, index, position1);
        int position2 = (int)bw.Position;
        bw.FillInt32("ParamHeaderOffsetsOffset", position2);
        for (int index = 0; index < Groups.Count; ++index)
            Groups[index].WriteParamHeaderOffsets(bw, index, position2);
        int position3 = (int)bw.Position;
        bw.FillInt32("ParamHeadersOffset", position3);
        for (int index = 0; index < Groups.Count; ++index)
            Groups[index].WriteParamHeaders(bw, Game, index, position3);
        int position4 = (int)bw.Position;
        bw.FillInt32("ValuesOffset", position4);
        for (int index = 0; index < Groups.Count; ++index)
            Groups[index].WriteValues(bw, index, position4);
        int position5 = (int)bw.Position;
        bw.FillInt32("ValueIDsOffset", (int)bw.Position);
        for (int index = 0; index < Groups.Count; ++index)
            Groups[index].WriteValueIDs(bw, Game, index, position5);
        bw.FillInt32("UnkOffset2", (int)bw.Position);
        bw.WriteBytes(UnkBlock2);
        bw.FillInt32("UnkOffset3", (int)bw.Position);
        for (int index = 0; index < Unk3s.Count; ++index)
            Unk3s[index].WriteHeader(bw, Game, index);
        int position6 = (int)bw.Position;
        bw.FillInt32("Unk3ValuesOffset", position6);
        for (int index = 0; index < Unk3s.Count; ++index)
            Unk3s[index].WriteValues(bw, Game, index, position6);
        if (Game != GPGame.DarkSouls3 && Game != GPGame.Sekiro)
            return;
        bw.FillInt32("CommentOffsetsOffsetsOffset", (int)bw.Position);
        for (int index = 0; index < Groups.Count; ++index)
            Groups[index].WriteCommentOffsetsOffset(bw, index);
        int position7 = (int)bw.Position;
        bw.FillInt32("CommentOffsetsOffset", position7);
        for (int index = 0; index < Groups.Count; ++index)
            Groups[index].WriteCommentOffsets(bw, index, position7);
        int position8 = (int)bw.Position;
        bw.FillInt32("CommentsOffset", position8);
        for (int index = 0; index < Groups.Count; ++index)
            Groups[index].WriteComments(bw, index, position8);
    }

    public Group this[string name1] => Groups.Find((Predicate<Group>)(group => group.Name1 == name1));

    public enum GPGame : uint
    {
        DarkSouls2 = 2,
        DarkSouls3 = 3,
        Sekiro = 5,
    }

    internal struct Offsets
    {
        public int GroupHeaders;
        public int ParamHeaderOffsets;
        public int ParamHeaders;
        public int Values;
        public int ValueIDs;
        public int Unk2;
        public int Unk3;
        public int Unk3ValueIDs;
        public int CommentOffsetsOffsets;
        public int CommentOffsets;
        public int Comments;
    }

    public class Group
    {
        public string Name1;
        public string Name2;
        public List<Param> Params;
        public List<string> Comments;

        public Group(string name1, string name2)
        {
            Name1 = name1;
            Name2 = name2;
            Params = new List<Param>();
            Comments = new List<string>();
        }

        internal Group(BinaryReaderEx br, GPGame game, int index, Offsets offsets)
        {
            int num1 = br.ReadInt32();
            br.StepIn(offsets.GroupHeaders + num1);
            int capacity = br.ReadInt32();
            int num2 = br.ReadInt32();
            if (game == GPGame.DarkSouls2)
            {
                Name1 = br.ReadShiftJIS();
            }
            else
            {
                Name1 = br.ReadUTF16();
                Name2 = br.ReadUTF16();
            }

            br.StepIn(offsets.ParamHeaderOffsets + num2);
            Params = new List<Param>(capacity);
            for (int index1 = 0; index1 < capacity; ++index1)
                Params.Add(new Param(br, game, offsets));
            br.StepOut();
            br.StepOut();
            Comments = new List<string>();
        }

        internal void WriteHeaderOffset(BinaryWriterEx bw, int groupIndex)
        {
            BinaryWriterEx binaryWriterEx = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 1);
            interpolatedStringHandler.AppendLiteral("GroupHeaderOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            binaryWriterEx.ReserveInt32(stringAndClear);
        }

        internal void WriteHeader(
            BinaryWriterEx bw,
            GPGame game,
            int groupIndex,
            int groupHeadersOffset)
        {
            BinaryWriterEx binaryWriterEx1 = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 1);
            interpolatedStringHandler.AppendLiteral("GroupHeaderOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            string stringAndClear1 = interpolatedStringHandler.ToStringAndClear();
            int num = (int)bw.Position - groupHeadersOffset;
            binaryWriterEx1.FillInt32(stringAndClear1, num);
            bw.WriteInt32(Params.Count);
            BinaryWriterEx binaryWriterEx2 = bw;
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
            interpolatedStringHandler.AppendLiteral("ParamHeaderOffsetsOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            string stringAndClear2 = interpolatedStringHandler.ToStringAndClear();
            binaryWriterEx2.ReserveInt32(stringAndClear2);
            if (game == GPGame.DarkSouls2)
            {
                bw.WriteShiftJIS(Name1, true);
            }
            else
            {
                bw.WriteUTF16(Name1, true);
                bw.WriteUTF16(Name2, true);
            }

            bw.Pad(4);
        }

        internal void WriteParamHeaderOffsets(
            BinaryWriterEx bw,
            int groupIndex,
            int paramHeaderOffsetsOffset)
        {
            BinaryWriterEx binaryWriterEx = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
            interpolatedStringHandler.AppendLiteral("ParamHeaderOffsetsOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            int num = (int)bw.Position - paramHeaderOffsetsOffset;
            binaryWriterEx.FillInt32(stringAndClear, num);
            for (int index = 0; index < Params.Count; ++index)
                Params[index].WriteParamHeaderOffset(bw, groupIndex, index);
        }

        internal void WriteParamHeaders(
            BinaryWriterEx bw,
            GPGame game,
            int groupindex,
            int paramHeadersOffset)
        {
            for (int index = 0; index < Params.Count; ++index)
                Params[index].WriteParamHeader(bw, game, groupindex, index, paramHeadersOffset);
        }

        internal void WriteValues(BinaryWriterEx bw, int groupindex, int valuesOffset)
        {
            for (int index = 0; index < Params.Count; ++index)
                Params[index].WriteValues(bw, groupindex, index, valuesOffset);
        }

        internal void WriteValueIDs(
            BinaryWriterEx bw,
            GPGame game,
            int groupIndex,
            int valueIDsOffset)
        {
            for (int index = 0; index < Params.Count; ++index)
                Params[index].WriteValueIDs(bw, game, groupIndex, index, valueIDsOffset);
        }

        internal void WriteCommentOffsetsOffset(BinaryWriterEx bw, int index)
        {
            BinaryWriterEx binaryWriterEx = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
            interpolatedStringHandler.AppendLiteral("CommentOffsetsOffset");
            interpolatedStringHandler.AppendFormatted(index);
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            binaryWriterEx.ReserveInt32(stringAndClear);
        }

        internal void WriteCommentOffsets(BinaryWriterEx bw, int index, int commentOffsetsOffset)
        {
            BinaryWriterEx binaryWriterEx1 = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
            interpolatedStringHandler.AppendLiteral("CommentOffsetsOffset");
            interpolatedStringHandler.AppendFormatted(index);
            string stringAndClear1 = interpolatedStringHandler.ToStringAndClear();
            int num = (int)bw.Position - commentOffsetsOffset;
            binaryWriterEx1.FillInt32(stringAndClear1, num);
            for (int index1 = 0; index1 < Comments.Count; ++index1)
            {
                BinaryWriterEx binaryWriterEx2 = bw;
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 2);
                interpolatedStringHandler.AppendLiteral("CommentOffset");
                interpolatedStringHandler.AppendFormatted(index);
                interpolatedStringHandler.AppendLiteral(":");
                interpolatedStringHandler.AppendFormatted(index1);
                string stringAndClear2 = interpolatedStringHandler.ToStringAndClear();
                binaryWriterEx2.ReserveInt32(stringAndClear2);
            }
        }

        internal void WriteComments(BinaryWriterEx bw, int index, int commentsOffset)
        {
            for (int index1 = 0; index1 < Comments.Count; ++index1)
            {
                BinaryWriterEx binaryWriterEx = bw;
                DefaultInterpolatedStringHandler
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 2);
                interpolatedStringHandler.AppendLiteral("CommentOffset");
                interpolatedStringHandler.AppendFormatted(index);
                interpolatedStringHandler.AppendLiteral(":");
                interpolatedStringHandler.AppendFormatted(index1);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                int num = (int)bw.Position - commentsOffset;
                binaryWriterEx.FillInt32(stringAndClear, num);
                bw.WriteUTF16(Comments[index1], true);
                bw.Pad(4);
            }
        }

        public Param this[string name1] => Params.Find((Predicate<Param>)(param => param.Name1 == name1));

        public override string ToString() => Name2 == null ? Name1 : Name1 + " | " + Name2;
    }

    public enum ParamType : byte
    {
        Byte = 1,
        Short = 2,
        IntA = 3,
        BoolA = 5,
        IntB = 7,
        Float = 9,
        BoolB = 11, // 0x0B
        Float2 = 12, // 0x0C
        Float3 = 13, // 0x0D
        Float4 = 14, // 0x0E
        Byte4 = 15, // 0x0F
    }

    public class Param
    {
        public string Name1;
        public string Name2;
        public ParamType Type;
        public List<object> Values;
        public List<int> ValueIDs;
        public List<float> TimeOfDay;

        public Param(string name1, string name2, ParamType type)
        {
            Name1 = name1;
            Name2 = name2;
            Type = type;
            Values = new List<object>();
            ValueIDs = new List<int>();
            TimeOfDay = null;
        }

        internal Param(BinaryReaderEx br, GPGame game, Offsets offsets)
        {
            int num1 = br.ReadInt32();
            br.StepIn(offsets.ParamHeaders + num1);
            int num2 = br.ReadInt32();
            int num3 = br.ReadInt32();
            Type = br.ReadEnum8<ParamType>();
            byte capacity = br.ReadByte();
            int num4 = br.AssertByte(new byte[1]);
            int num5 = br.AssertByte(new byte[1]);
            if (Type == ParamType.Byte && capacity > 1)
                throw new Exception("Notify TKGP so he can look into this, please.");
            if (game == GPGame.DarkSouls2)
            {
                Name1 = br.ReadShiftJIS();
            }
            else
            {
                Name1 = br.ReadUTF16();
                Name2 = br.ReadUTF16();
            }

            br.StepIn(offsets.Values + num2);
            Values = new List<object>(capacity);
            for (int index = 0; index < capacity; ++index)
            {
                switch (Type)
                {
                    case ParamType.Byte:
                        Values.Add(br.ReadByte());
                        break;
                    case ParamType.Short:
                        Values.Add(br.ReadInt16());
                        break;
                    case ParamType.IntA:
                        Values.Add(br.ReadInt32());
                        break;
                    case ParamType.BoolA:
                        Values.Add(br.ReadBoolean());
                        break;
                    case ParamType.IntB:
                        Values.Add(br.ReadInt32());
                        break;
                    case ParamType.Float:
                        Values.Add(br.ReadSingle());
                        break;
                    case ParamType.BoolB:
                        Values.Add(br.ReadBoolean());
                        break;
                    case ParamType.Float2:
                        Values.Add(br.ReadVector2());
                        br.AssertInt32(new int[1]);
                        br.AssertInt32(new int[1]);
                        break;
                    case ParamType.Float3:
                        Values.Add(br.ReadVector3());
                        br.AssertInt32(new int[1]);
                        break;
                    case ParamType.Float4:
                        Values.Add(br.ReadVector4());
                        break;
                    case ParamType.Byte4:
                        Values.Add(br.ReadBytes(4));
                        break;
                }
            }

            br.StepOut();
            br.StepIn(offsets.ValueIDs + num3);
            ValueIDs = new List<int>(capacity);
            TimeOfDay = game != GPGame.Sekiro ? null : new List<float>(capacity);
            for (int index = 0; index < capacity; ++index)
            {
                ValueIDs.Add(br.ReadInt32());
                if (game == GPGame.Sekiro)
                    TimeOfDay.Add(br.ReadSingle());
            }

            br.StepOut();
            br.StepOut();
        }

        internal void WriteParamHeaderOffset(BinaryWriterEx bw, int groupIndex, int paramIndex)
        {
            BinaryWriterEx binaryWriterEx = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 2);
            interpolatedStringHandler.AppendLiteral("ParamHeaderOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            interpolatedStringHandler.AppendLiteral(":");
            interpolatedStringHandler.AppendFormatted(paramIndex);
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            binaryWriterEx.ReserveInt32(stringAndClear);
        }

        internal void WriteParamHeader(
            BinaryWriterEx bw,
            GPGame game,
            int groupIndex,
            int paramIndex,
            int paramHeadersOffset)
        {
            BinaryWriterEx binaryWriterEx1 = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 2);
            interpolatedStringHandler.AppendLiteral("ParamHeaderOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            interpolatedStringHandler.AppendLiteral(":");
            interpolatedStringHandler.AppendFormatted(paramIndex);
            string stringAndClear1 = interpolatedStringHandler.ToStringAndClear();
            int num = (int)bw.Position - paramHeadersOffset;
            binaryWriterEx1.FillInt32(stringAndClear1, num);
            BinaryWriterEx binaryWriterEx2 = bw;
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 2);
            interpolatedStringHandler.AppendLiteral("ValuesOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            interpolatedStringHandler.AppendLiteral(":");
            interpolatedStringHandler.AppendFormatted(paramIndex);
            string stringAndClear2 = interpolatedStringHandler.ToStringAndClear();
            binaryWriterEx2.ReserveInt32(stringAndClear2);
            BinaryWriterEx binaryWriterEx3 = bw;
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 2);
            interpolatedStringHandler.AppendLiteral("ValueIDsOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            interpolatedStringHandler.AppendLiteral(":");
            interpolatedStringHandler.AppendFormatted(paramIndex);
            string stringAndClear3 = interpolatedStringHandler.ToStringAndClear();
            binaryWriterEx3.ReserveInt32(stringAndClear3);
            bw.WriteByte((byte)Type);
            bw.WriteByte((byte)Values.Count);
            bw.WriteByte(0);
            bw.WriteByte(0);
            if (game == GPGame.DarkSouls2)
            {
                bw.WriteShiftJIS(Name1, true);
            }
            else
            {
                bw.WriteUTF16(Name1, true);
                bw.WriteUTF16(Name2, true);
            }

            bw.Pad(4);
        }

        internal void WriteValues(
            BinaryWriterEx bw,
            int groupIndex,
            int paramIndex,
            int valuesOffset)
        {
            BinaryWriterEx binaryWriterEx = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 2);
            interpolatedStringHandler.AppendLiteral("ValuesOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            interpolatedStringHandler.AppendLiteral(":");
            interpolatedStringHandler.AppendFormatted(paramIndex);
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            int num = (int)bw.Position - valuesOffset;
            binaryWriterEx.FillInt32(stringAndClear, num);
            for (int index = 0; index < Values.Count; ++index)
            {
                object obj = Values[index];
                switch (Type)
                {
                    case ParamType.Byte:
                        bw.WriteInt32((byte)obj);
                        break;
                    case ParamType.Short:
                        bw.WriteInt16((short)obj);
                        break;
                    case ParamType.IntA:
                        bw.WriteInt32((int)obj);
                        break;
                    case ParamType.BoolA:
                        bw.WriteBoolean((bool)obj);
                        break;
                    case ParamType.IntB:
                        bw.WriteInt32((int)obj);
                        break;
                    case ParamType.Float:
                        bw.WriteSingle((float)obj);
                        break;
                    case ParamType.BoolB:
                        bw.WriteBoolean((bool)obj);
                        break;
                    case ParamType.Float2:
                        bw.WriteVector2((Vector2)obj);
                        bw.WriteInt32(0);
                        bw.WriteInt32(0);
                        break;
                    case ParamType.Float3:
                        bw.WriteVector3((Vector3)obj);
                        bw.WriteInt32(0);
                        break;
                    case ParamType.Float4:
                        bw.WriteVector4((Vector4)obj);
                        break;
                    case ParamType.Byte4:
                        bw.WriteBytes((byte[])obj);
                        break;
                }
            }

            bw.Pad(4);
        }

        internal void WriteValueIDs(
            BinaryWriterEx bw,
            GPGame game,
            int groupIndex,
            int paramIndex,
            int valueIDsOffset)
        {
            BinaryWriterEx binaryWriterEx = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 2);
            interpolatedStringHandler.AppendLiteral("ValueIDsOffset");
            interpolatedStringHandler.AppendFormatted(groupIndex);
            interpolatedStringHandler.AppendLiteral(":");
            interpolatedStringHandler.AppendFormatted(paramIndex);
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            int num = (int)bw.Position - valueIDsOffset;
            binaryWriterEx.FillInt32(stringAndClear, num);
            for (int index = 0; index < ValueIDs.Count; ++index)
            {
                bw.WriteInt32(ValueIDs[index]);
                if (game == GPGame.Sekiro)
                    bw.WriteSingle(TimeOfDay[index]);
            }
        }

        public object this[int index]
        {
            get => Values[index];
            set => Values[index] = value;
        }

        public override string ToString() => Name2 == null ? Name1 : Name1 + " | " + Name2;
    }

    public class Unk3
    {
        public int GroupIndex;
        public List<int> ValueIDs;
        public int Unk0C;

        public Unk3(int groupIndex)
        {
            GroupIndex = groupIndex;
            ValueIDs = new List<int>();
        }

        internal Unk3(BinaryReaderEx br, GPGame game, Offsets offsets)
        {
            GroupIndex = br.ReadInt32();
            int count = br.ReadInt32();
            uint num = br.ReadUInt32();
            if (game == GPGame.Sekiro)
                Unk0C = br.ReadInt32();
            ValueIDs = new List<int>(br.GetInt32s(offsets.Unk3ValueIDs + num, count));
        }

        internal void WriteHeader(BinaryWriterEx bw, GPGame game, int index)
        {
            bw.WriteInt32(GroupIndex);
            bw.WriteInt32(ValueIDs.Count);
            BinaryWriterEx binaryWriterEx = bw;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
            interpolatedStringHandler.AppendLiteral("Unk3ValueIDsOffset");
            interpolatedStringHandler.AppendFormatted(index);
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            binaryWriterEx.ReserveInt32(stringAndClear);
            if (game != GPGame.Sekiro)
                return;
            bw.WriteInt32(Unk0C);
        }

        internal void WriteValues(
            BinaryWriterEx bw,
            GPGame game,
            int index,
            int unk3ValueIDsOffset)
        {
            if (ValueIDs.Count == 0)
            {
                BinaryWriterEx binaryWriterEx = bw;
                DefaultInterpolatedStringHandler
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
                interpolatedStringHandler.AppendLiteral("Unk3ValueIDsOffset");
                interpolatedStringHandler.AppendFormatted(index);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                binaryWriterEx.FillInt32(stringAndClear, 0);
            }
            else
            {
                BinaryWriterEx binaryWriterEx = bw;
                DefaultInterpolatedStringHandler
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
                interpolatedStringHandler.AppendLiteral("Unk3ValueIDsOffset");
                interpolatedStringHandler.AppendFormatted(index);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                int num = (int)bw.Position - unk3ValueIDsOffset;
                binaryWriterEx.FillInt32(stringAndClear, num);
                bw.WriteInt32s(ValueIDs);
            }
        }
    }
}