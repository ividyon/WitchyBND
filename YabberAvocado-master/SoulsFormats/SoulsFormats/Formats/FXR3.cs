using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System;

namespace SoulsFormats
{
    /// <summary>
    /// An SFX definition file used in DS3 and Sekiro. Extension: .fxr
    /// </summary>
    public class FXR3 : SoulsFile<FXR3>
    {
        public FXRVersion Version { get; set; }

        public int ID { get; set; }

        public FFXStateMachine RootStateMachine { get; set; }

        public FFXEffectCallA RootEffectCall { get; set; }

        public List<int> Section12s { get; set; }

        public List<int> Section13s { get; set; }

        public FXR3()
        {
            Version = FXRVersion.DarkSouls3;
            RootStateMachine = new FFXStateMachine();
            RootEffectCall = new FFXEffectCallA();
            Section12s = new List<int>();
            Section13s = new List<int>();
        }

        protected override bool Is(BinaryReaderEx br)
        {
            if (br.Length < 8L)
                return false;
            string ascii = br.GetASCII(0L, 4);
            short int16 = br.GetInt16(6L);
            return ascii == "FXR\0" && (int16 == (short)4 || int16 == (short)5);
        }

        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = false;
            br.AssertASCII("FXR\0");
            int num1 = (int)br.AssertInt16(new short[1]);
            Version = br.ReadEnum16<FXRVersion>();
            br.AssertInt32(1);
            ID = br.ReadInt32();
            int num2 = br.ReadInt32();
            br.AssertInt32(1);
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            int num3 = br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.ReadInt32();
            br.AssertInt32(1);
            br.AssertInt32(new int[1]);
            if (Version == FXRVersion.Sekiro)
            {
                int num4 = br.ReadInt32();
                int count1 = br.ReadInt32();
                int num5 = br.ReadInt32();
                int count2 = br.ReadInt32();
                br.ReadInt32();
                br.AssertInt32(new int[] { 0, 1 });
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                Section12s = new List<int>((IEnumerable<int>)br.GetInt32s((long)num4, count1));
                Section13s = new List<int>((IEnumerable<int>)br.GetInt32s((long)num5, count2));
            }
            else
            {
                Section12s = new List<int>();
                Section13s = new List<int>();
            }
            br.Position = (long)num2;
            RootStateMachine = new FFXStateMachine(br);
            br.Position = (long)num3;
            RootEffectCall = new FFXEffectCallA(br);
        }

        protected override void Write(BinaryWriterEx bw)
        {
            bw.WriteASCII("FXR\0");
            bw.WriteInt16((short)0);
            bw.WriteUInt16((ushort)Version);
            bw.WriteInt32(1);
            bw.WriteInt32(ID);
            bw.ReserveInt32("Section1Offset");
            bw.WriteInt32(1);
            bw.ReserveInt32("Section2Offset");
            bw.WriteInt32(RootStateMachine.States.Count);
            bw.ReserveInt32("Section3Offset");
            bw.ReserveInt32("Section3Count");
            bw.ReserveInt32("Section4Offset");
            bw.ReserveInt32("Section4Count");
            bw.ReserveInt32("Section5Offset");
            bw.ReserveInt32("Section5Count");
            bw.ReserveInt32("Section6Offset");
            bw.ReserveInt32("Section6Count");
            bw.ReserveInt32("Section7Offset");
            bw.ReserveInt32("Section7Count");
            bw.ReserveInt32("Section8Offset");
            bw.ReserveInt32("Section8Count");
            bw.ReserveInt32("Section9Offset");
            bw.ReserveInt32("Section9Count");
            bw.ReserveInt32("Section10Offset");
            bw.ReserveInt32("Section10Count");
            bw.ReserveInt32("Section11Offset");
            bw.ReserveInt32("Section11Count");
            bw.WriteInt32(1);
            bw.WriteInt32(0);
            if (Version == FXRVersion.Sekiro)
            {
                bw.ReserveInt32("Section12Offset");
                bw.WriteInt32(Section12s.Count);
                bw.ReserveInt32("Section13Offset");
                bw.WriteInt32(Section13s.Count);
                bw.ReserveInt32("Section14Offset");
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }
            bw.FillInt32("Section1Offset", (int)bw.Position);
            RootStateMachine.Write(bw);
            bw.Pad(16);
            bw.FillInt32("Section2Offset", (int)bw.Position);
            RootStateMachine.WriteSection2s(bw);
            bw.Pad(16);
            bw.FillInt32("Section3Offset", (int)bw.Position);
            List<FFXState> states = RootStateMachine.States;
            List<FFXTransition> section3s = new List<FFXTransition>();
            for (int index = 0; index < states.Count; ++index)
                states[index].WriteSection3s(bw, index, section3s);
            bw.FillInt32("Section3Count", section3s.Count);
            bw.Pad(16);
            bw.FillInt32("Section4Offset", (int)bw.Position);
            List<FFXEffectCallA> section4s = new List<FFXEffectCallA>();
            RootEffectCall.Write(bw, section4s);
            RootEffectCall.WriteSection4s(bw, section4s);
            bw.FillInt32("Section4Count", section4s.Count);
            bw.Pad(16);
            bw.FillInt32("Section5Offset", (int)bw.Position);
            int section5Count = 0;
            for (int index = 0; index < section4s.Count; ++index)
                section4s[index].WriteSection5s(bw, index, ref section5Count);
            bw.FillInt32("Section5Count", section5Count);
            bw.Pad(16);
            bw.FillInt32("Section6Offset", (int)bw.Position);
            section5Count = 0;
            List<FFXActionCall> section6s = new List<FFXActionCall>();
            for (int index = 0; index < section4s.Count; ++index)
                section4s[index].WriteSection6s(bw, index, ref section5Count, section6s);
            bw.FillInt32("Section6Count", section6s.Count);
            bw.Pad(16);
            bw.FillInt32("Section7Offset", (int)bw.Position);
            List<FFXProperty> section7s = new List<FFXProperty>();
            for (int index = 0; index < section6s.Count; ++index)
                section6s[index].WriteSection7s(bw, index, section7s);
            bw.FillInt32("Section7Count", section7s.Count);
            bw.Pad(16);
            bw.FillInt32("Section8Offset", (int)bw.Position);
            List<Section8> section8s = new List<Section8>();
            for (int index = 0; index < section7s.Count; ++index)
                section7s[index].WriteSection8s(bw, index, section8s);
            bw.FillInt32("Section8Count", section8s.Count);
            bw.Pad(16);
            bw.FillInt32("Section9Offset", (int)bw.Position);
            List<Section9> section9s = new List<Section9>();
            for (int index = 0; index < section8s.Count; ++index)
                section8s[index].WriteSection9s(bw, index, section9s);
            bw.FillInt32("Section9Count", section9s.Count);
            bw.Pad(16);
            bw.FillInt32("Section10Offset", (int)bw.Position);
            List<Section10> section10s = new List<Section10>();
            for (int index = 0; index < section6s.Count; ++index)
                section6s[index].WriteSection10s(bw, index, section10s);
            bw.FillInt32("Section10Count", section10s.Count);
            bw.Pad(16);
            bw.FillInt32("Section11Offset", (int)bw.Position);
            int section11Count = 0;
            for (int index = 0; index < section3s.Count; ++index)
                section3s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section6s.Count; ++index)
                section6s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section7s.Count; ++index)
                section7s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section8s.Count; ++index)
                section8s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section9s.Count; ++index)
                section9s[index].WriteSection11s(bw, index, ref section11Count);
            for (int index = 0; index < section10s.Count; ++index)
                section10s[index].WriteSection11s(bw, index, ref section11Count);
            bw.FillInt32("Section11Count", section11Count);
            bw.Pad(16);
            if (Version != FXRVersion.Sekiro)
                return;
            bw.FillInt32("Section12Offset", (int)bw.Position);
            bw.WriteInt32s((IList<int>)Section12s);
            bw.Pad(16);
            bw.FillInt32("Section13Offset", (int)bw.Position);
            bw.WriteInt32s((IList<int>)Section13s);
            bw.Pad(16);
            bw.FillInt32("Section14Offset", (int)bw.Position);
        }

        public enum FXRVersion : ushort
        {
            DarkSouls3 = 4,
            Sekiro = 5,
        }

        public class FFXStateMachine
        {
            public List<FFXState> States { get; set; }

            public FFXStateMachine() => States = new List<FFXState>();

            internal FFXStateMachine(BinaryReaderEx br)
            {
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num);
                States = new List<FFXState>(capacity);
                for (int index = 0; index < capacity; ++index)
                    States.Add(new FFXState(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(States.Count);
                bw.ReserveInt32("Section1Section2sOffset");
                bw.WriteInt32(0);
            }

            internal void WriteSection2s(BinaryWriterEx bw)
            {
                bw.FillInt32("Section1Section2sOffset", (int)bw.Position);
                for (int index = 0; index < States.Count; ++index)
                    States[index].Write(bw, index);
            }
        }

        public class FFXState
        {
            public List<FFXTransition> Transitions { get; set; }

            public FFXState() => Transitions = new List<FFXTransition>();

            internal FFXState(BinaryReaderEx br)
            {
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num);
                Transitions = new List<FFXTransition>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Transitions.Add(new FFXTransition(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(Transitions.Count);
                bw.ReserveInt32(string.Format("Section2Section3sOffset[{0}]", (object)index));
                bw.WriteInt32(0);
            }

            internal void WriteSection3s(
              BinaryWriterEx bw,
              int index,
              List<FFXTransition> section3s)
            {
                bw.FillInt32(string.Format("Section2Section3sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FFXTransition transition in Transitions)
                    transition.Write(bw, section3s);
            }
        }

        public class FFXTransition
        {
            [XmlAttribute]
            public int TargetStateIndex { get; set; }

            public int Unk10 { get; set; }

            public int Unk38 { get; set; }

            public int Section11Data1 { get; set; }

            public int Section11Data2 { get; set; }

            public FFXTransition()
            {
            }

            internal FFXTransition(BinaryReaderEx br)
            {
                int num1 = (int)br.AssertInt16((short)8, (short)10, (short)11);
                int num2 = (int)br.AssertByte(new byte[1]);
                int num3 = (int)br.AssertByte((byte)1);
                br.AssertInt32(new int[1]);
                TargetStateIndex = br.ReadInt32();
                br.AssertInt32(new int[1]);
                Unk10 = br.AssertInt32(16842748, 16842749, 16842750, 16842751);
                br.AssertInt32(new int[1]);
                br.AssertInt32(1);
                br.AssertInt32(new int[1]);
                int num4 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                Unk38 = br.AssertInt32(16842748, 16842749, 16842750, 16842751);
                br.AssertInt32(new int[1]);
                br.AssertInt32(1, 0);
                br.AssertInt32(new int[1]);
                int num5 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                Section11Data1 = br.GetInt32((long)num4);
                Section11Data2 = br.GetInt32((long)num5);
            }

            internal void Write(BinaryWriterEx bw, List<FFXTransition> section3s)
            {
                int count = section3s.Count;
                bw.WriteInt16((short)11);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(0);
                bw.WriteInt32(TargetStateIndex);
                bw.WriteInt32(0);
                bw.WriteInt32(Unk10);
                bw.WriteInt32(0);
                bw.WriteInt32(1);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section3Section11Offset1[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(Unk38);
                bw.WriteInt32(0);
                bw.WriteInt32(1);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section3Section11Offset2[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                section3s.Add(this);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section3Section11Offset1[{0}]", (object)index), (int)bw.Position);
                bw.WriteInt32(Section11Data1);
                bw.FillInt32(string.Format("Section3Section11Offset2[{0}]", (object)index), (int)bw.Position);
                bw.WriteInt32(Section11Data2);
                section11Count += 2;
            }
        }

        public class FFXEffectCallA
        {
            [XmlAttribute]
            public short EffectID { get; set; }

            public List<FFXEffectCallA> EffectAs { get; set; }

            public List<FFXEffectCallB> EffectBs { get; set; }

            public List<FFXActionCall> Actions { get; set; }

            public FFXEffectCallA()
            {
                EffectAs = new List<FFXEffectCallA>();
                EffectBs = new List<FFXEffectCallB>();
                Actions = new List<FFXActionCall>();
            }

            internal FFXEffectCallA(BinaryReaderEx br)
            {
                EffectID = br.ReadInt16();
                int num1 = (int)br.AssertByte(new byte[1]);
                int num2 = (int)br.AssertByte((byte)1);
                br.AssertInt32(new int[1]);
                int capacity1 = br.ReadInt32();
                int capacity2 = br.ReadInt32();
                int capacity3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num4 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num5 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num5);
                EffectAs = new List<FFXEffectCallA>(capacity3);
                for (int index = 0; index < capacity3; ++index)
                    EffectAs.Add(new FFXEffectCallA(br));
                br.StepOut();
                br.StepIn((long)num3);
                EffectBs = new List<FFXEffectCallB>(capacity1);
                for (int index = 0; index < capacity1; ++index)
                    EffectBs.Add(new FFXEffectCallB(br));
                br.StepOut();
                br.StepIn((long)num4);
                Actions = new List<FFXActionCall>(capacity2);
                for (int index = 0; index < capacity2; ++index)
                    Actions.Add(new FFXActionCall(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, List<FFXEffectCallA> section4s)
            {
                int count = section4s.Count;
                bw.WriteInt16(EffectID);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(0);
                bw.WriteInt32(EffectBs.Count);
                bw.WriteInt32(Actions.Count);
                bw.WriteInt32(EffectAs.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section4Section5sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section4Section6sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section4Section4sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                section4s.Add(this);
            }

            internal void WriteSection4s(BinaryWriterEx bw, List<FFXEffectCallA> section4s)
            {
                int num = section4s.IndexOf(this);
                if (EffectAs.Count == 0)
                {
                    bw.FillInt32(string.Format("Section4Section4sOffset[{0}]", (object)num), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section4Section4sOffset[{0}]", (object)num), (int)bw.Position);
                    foreach (FFXEffectCallA effectA in EffectAs)
                        effectA.Write(bw, section4s);
                    foreach (FFXEffectCallA effectA in EffectAs)
                        effectA.WriteSection4s(bw, section4s);
                }
            }

            internal void WriteSection5s(BinaryWriterEx bw, int index, ref int section5Count)
            {
                if (EffectBs.Count == 0)
                {
                    bw.FillInt32(string.Format("Section4Section5sOffset[{0}]", (object)index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section4Section5sOffset[{0}]", (object)index), (int)bw.Position);
                    for (int index1 = 0; index1 < EffectBs.Count; ++index1)
                        EffectBs[index1].Write(bw, section5Count + index1);
                    section5Count += EffectBs.Count;
                }
            }

            internal void WriteSection6s(
              BinaryWriterEx bw,
              int index,
              ref int section5Count,
              List<FFXActionCall> section6s)
            {
                bw.FillInt32(string.Format("Section4Section6sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FFXActionCall action in Actions)
                    action.Write(bw, section6s);
                for (int index1 = 0; index1 < EffectBs.Count; ++index1)
                    EffectBs[index1].WriteSection6s(bw, section5Count + index1, section6s);
                section5Count += EffectBs.Count;
            }
        }

        public class FFXEffectCallB
        {
            [XmlAttribute]
            public short EffectID { get; set; }

            public List<FFXActionCall> Actions { get; set; }

            public FFXEffectCallB() => Actions = new List<FFXActionCall>();

            internal FFXEffectCallB(BinaryReaderEx br)
            {
                EffectID = br.ReadInt16();
                int num1 = (int)br.AssertByte(new byte[1]);
                int num2 = (int)br.AssertByte((byte)1);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num3);
                Actions = new List<FFXActionCall>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Actions.Add(new FFXActionCall(br));
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt16(EffectID);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(Actions.Count);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section5Section6sOffset[{0}]", (object)index));
                bw.WriteInt32(0);
            }

            internal void WriteSection6s(
              BinaryWriterEx bw,
              int index,
              List<FFXActionCall> section6s)
            {
                bw.FillInt32(string.Format("Section5Section6sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FFXActionCall action in Actions)
                    action.Write(bw, section6s);
            }
        }

        public class FFXActionCall
        {
            [XmlAttribute]
            public short ActionID { get; set; }

            public bool Unk02 { get; set; }

            public bool Unk03 { get; set; }

            public int Unk04 { get; set; }

            public List<FFXProperty> Properties1 { get; set; }

            public List<FFXProperty> Properties2 { get; set; }

            public List<Section10> Section10s { get; set; }

            public List<FFXField> Fields1 { get; set; }

            public List<FFXField> Fields2 { get; set; }

            public FFXActionCall()
            {
                Properties1 = new List<FFXProperty>();
                Properties2 = new List<FFXProperty>();
                Section10s = new List<Section10>();
                Fields1 = new List<FFXField>();
                Fields2 = new List<FFXField>();
            }

            internal FFXActionCall(BinaryReaderEx br)
            {
                ActionID = br.ReadInt16();
                Unk02 = br.ReadBoolean();
                Unk03 = br.ReadBoolean();
                Unk04 = br.ReadInt32();
                int count1 = br.ReadInt32();
                int capacity1 = br.ReadInt32();
                int capacity2 = br.ReadInt32();
                int count2 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int capacity3 = br.ReadInt32();
                int num1 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num2 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.AssertInt32(new int[1]);
                br.StepIn((long)num3);
                Properties1 = new List<FFXProperty>(capacity2);
                for (int index = 0; index < capacity2; ++index)
                    Properties1.Add(new FFXProperty(br));
                Properties2 = new List<FFXProperty>(capacity3);
                for (int index = 0; index < capacity3; ++index)
                    Properties2.Add(new FFXProperty(br));
                br.StepOut();
                br.StepIn((long)num2);
                Section10s = new List<Section10>(capacity1);
                for (int index = 0; index < capacity1; ++index)
                    Section10s.Add(new Section10(br));
                br.StepOut();
                br.StepIn((long)num1);
                Fields1 = FFXField.ReadMany(br, count1);
                Fields2 = FFXField.ReadMany(br, count2);
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, List<FFXActionCall> section6s)
            {
                int count = section6s.Count;
                bw.WriteInt16(ActionID);
                bw.WriteBoolean(Unk02);
                bw.WriteBoolean(Unk03);
                bw.WriteInt32(Unk04);
                bw.WriteInt32(Fields1.Count);
                bw.WriteInt32(Section10s.Count);
                bw.WriteInt32(Properties1.Count);
                bw.WriteInt32(Fields2.Count);
                bw.WriteInt32(0);
                bw.WriteInt32(Properties2.Count);
                bw.ReserveInt32(string.Format("Section6Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section6Section10sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section6Section7sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                section6s.Add(this);
            }

            internal void WriteSection7s(BinaryWriterEx bw, int index, List<FFXProperty> section7s)
            {
                bw.FillInt32(string.Format("Section6Section7sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FFXProperty ffxProperty in Properties1)
                    ffxProperty.Write(bw, section7s);
                foreach (FFXProperty ffxProperty in Properties2)
                    ffxProperty.Write(bw, section7s);
            }

            internal void WriteSection10s(BinaryWriterEx bw, int index, List<Section10> section10s)
            {
                bw.FillInt32(string.Format("Section6Section10sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Section10 section10 in Section10s)
                    section10.Write(bw, section10s);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                if (Fields1.Count == 0 && Fields2.Count == 0)
                {
                    bw.FillInt32(string.Format("Section6Section11sOffset[{0}]", (object)index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section6Section11sOffset[{0}]", (object)index), (int)bw.Position);
                    foreach (FFXField ffxField in Fields1)
                        ffxField.Write(bw);
                    foreach (FFXField ffxField in Fields2)
                        ffxField.Write(bw);
                    section11Count += Fields1.Count + Fields2.Count;
                }
            }
        }

        [XmlInclude(typeof(FFXField.FFXFieldFloat))]
        [XmlInclude(typeof(FFXField.FFXFieldInt))]
        public abstract class FFXField
        {
            public static FFXField Read(BinaryReaderEx br)
            {
                float single = br.GetSingle(br.Position);
                FFXField ffxField;
                if ((double)single >= 9.99999974737875E-05 && (double)single < 1000000.0 || (double)single <= -9.99999974737875E-05 && (double)single > -1000000.0)
                    ffxField = (FFXField)new FFXField.FFXFieldFloat()
                    {
                        Value = single
                    };
                else
                    ffxField = (FFXField)new FFXField.FFXFieldInt()
                    {
                        Value = br.GetInt32(br.Position)
                    };
                br.Position += 4L;
                return ffxField;
            }

            public static List<FFXField> ReadMany(BinaryReaderEx br, int count)
            {
                List<FFXField> ffxFieldList = new List<FFXField>();
                for (int index = 0; index < count; ++index)
                    ffxFieldList.Add(FFXField.Read(br));
                return ffxFieldList;
            }

            public static List<FFXField> ReadManyAt(
              BinaryReaderEx br,
              int offset,
              int count)
            {
                br.StepIn((long)offset);
                List<FFXField> ffxFieldList = FFXField.ReadMany(br, count);
                br.StepOut();
                return ffxFieldList;
            }

            public abstract void Write(BinaryWriterEx bw);

            public class FFXFieldFloat : FFXField
            {
                [XmlAttribute]
                public float Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteSingle(Value);
            }

            public class FFXFieldInt : FFXField
            {
                [XmlAttribute]
                public int Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteInt32(Value);
            }
        }

        public class FFXProperty
        {
            [XmlAttribute]
            public short TypeEnumA { get; set; }

            [XmlAttribute]
            public int TypeEnumB { get; set; }

            public List<Section8> Section8s { get; set; }

            public List<FFXField> Fields { get; set; }

            public FFXProperty()
            {
                Section8s = new List<Section8>();
                Fields = new List<FFXField>();
            }

            internal FFXProperty(BinaryReaderEx br)
            {
                TypeEnumA = br.ReadInt16();
                int num1 = (int)br.AssertByte(new byte[1]);
                int num2 = (int)br.AssertByte((byte)1);
                TypeEnumB = br.ReadInt32();
                int count = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int offset = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int capacity = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num3);
                Section8s = new List<Section8>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Section8s.Add(new Section8(br));
                br.StepOut();
                Fields = new List<FFXField>();
                Fields = FFXField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<FFXProperty> section7s)
            {
                int count = section7s.Count;
                bw.WriteInt16(TypeEnumA);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(TypeEnumB);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section7Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section7Section8sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(Section8s.Count);
                bw.WriteInt32(0);
                section7s.Add(this);
            }

            internal void WriteSection8s(BinaryWriterEx bw, int index, List<Section8> section8s)
            {
                bw.FillInt32(string.Format("Section7Section8sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Section8 section8 in Section8s)
                    section8.Write(bw, section8s);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                if (Fields.Count == 0)
                {
                    bw.FillInt32(string.Format("Section7Section11sOffset[{0}]", (object)index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("Section7Section11sOffset[{0}]", (object)index), (int)bw.Position);
                    foreach (FFXField field in Fields)
                        field.Write(bw);
                    section11Count += Fields.Count;
                }
            }
        }

        public class Section8
        {
            [XmlAttribute]
            public ushort Unk00 { get; set; }

            public int Unk04 { get; set; }

            public List<Section9> Section9s { get; set; }

            public List<FFXField> Fields { get; set; }

            public Section8()
            {
                Section9s = new List<Section9>();
                Fields = new List<FFXField>();
            }

            internal Section8(BinaryReaderEx br)
            {
                Unk00 = br.ReadUInt16();
                int num1 = (int)br.AssertByte(new byte[1]);
                int num2 = (int)br.AssertByte((byte)1);
                Unk04 = br.ReadInt32();
                int count = br.ReadInt32();
                int capacity = br.ReadInt32();
                int offset = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int num3 = br.ReadInt32();
                br.AssertInt32(new int[1]);
                br.StepIn((long)num3);
                Section9s = new List<Section9>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Section9s.Add(new Section9(br));
                br.StepOut();
                Fields = FFXField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<Section8> section8s)
            {
                int count = section8s.Count;
                bw.WriteUInt16(Unk00);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(Unk04);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(Section9s.Count);
                bw.ReserveInt32(string.Format("Section8Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section8Section9sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                section8s.Add(this);
            }

            internal void WriteSection9s(BinaryWriterEx bw, int index, List<Section9> section9s)
            {
                bw.FillInt32(string.Format("Section8Section9sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (Section9 section9 in Section9s)
                    section9.Write(bw, section9s);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section8Section11sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FFXField field in Fields)
                    field.Write(bw);
                section11Count += Fields.Count;
            }
        }

        public class Section9
        {
            public int Unk04 { get; set; }

            public List<FFXField> Fields { get; set; }

            public Section9() => Fields = new List<FFXField>();

            internal Section9(BinaryReaderEx br)
            {
                int num1 = (int)br.AssertInt16((short)48, (short)64, (short)67);
                int num2 = (int)br.AssertByte(new byte[1]);
                int num3 = (int)br.AssertByte((byte)1);
                Unk04 = br.ReadInt32();
                int count = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int offset = br.ReadInt32();
                br.AssertInt32(new int[1]);
                Fields = FFXField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<Section9> section9s)
            {
                int count = section9s.Count;
                bw.WriteInt16((short)48);
                bw.WriteByte((byte)0);
                bw.WriteByte((byte)1);
                bw.WriteInt32(Unk04);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section9Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                section9s.Add(this);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section9Section11sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FFXField field in Fields)
                    field.Write(bw);
                section11Count += Fields.Count;
            }
        }

        public class Section10
        {
            public List<FFXField> Fields { get; set; }

            public Section10() => Fields = new List<FFXField>();

            internal Section10(BinaryReaderEx br)
            {
                int offset = br.ReadInt32();
                br.AssertInt32(new int[1]);
                int count = br.ReadInt32();
                br.AssertInt32(new int[1]);
                Fields = FFXField.ReadManyAt(br, offset, count);
            }

            internal void Write(BinaryWriterEx bw, List<Section10> section10s)
            {
                int count = section10s.Count;
                bw.ReserveInt32(string.Format("Section10Section11sOffset[{0}]", (object)count));
                bw.WriteInt32(0);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(0);
                section10s.Add(this);
            }

            internal void WriteSection11s(BinaryWriterEx bw, int index, ref int section11Count)
            {
                bw.FillInt32(string.Format("Section10Section11sOffset[{0}]", (object)index), (int)bw.Position);
                foreach (FFXField field in Fields)
                    field.Write(bw);
                section11Count += Fields.Count;
            }
        }
    }
}
