using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SoulsFormats;

namespace WitchyFormats
{
    /// <summary>
    /// An SFX definition file used in DS3 and Sekiro. Extension: .fxr
    /// Maintained and reworked by ivi from groundwork laid by TKGP and NamelessHoodie.
    /// Still not byte-perfect, but fairly close, with only offsets not lining up, which is pretty whatever.
    /// </summary>
    public class Fxr3 : SoulsFile<Fxr3>
    {
        public FXRVersion Version { get; set; }

        public int Id { get; set; }

        public FFXStateMachine RootStateMachine { get; set; }

        public FFXEffectCallA RootEffectCall { get; set; }

        public List<int> Section12s { get; set; }

        public List<int> Section13s { get; set; }

        public List<int> Section14s { get; set; }

        public List<int> Section15s { get; set; }

        public Fxr3()
        {
            Version = FXRVersion.DarkSouls3;
            RootStateMachine = new FFXStateMachine();
            RootEffectCall = new FFXEffectCallA();
            Section12s = new List<int>();
            Section13s = new List<int>();
            Section14s = new List<int>();
            Section15s = new List<int>();
        }

        public Fxr3(Fxr3 fxr)
        {
            Id = fxr.Id;
            Version = fxr.Version;
            RootStateMachine = new FFXStateMachine(fxr.RootStateMachine);
            RootEffectCall = new FFXEffectCallA(fxr.RootEffectCall);
            Section12s = new List<int>(fxr.Section12s);
            Section13s = new List<int>(fxr.Section13s);
            Section14s = new List<int>(fxr.Section14s);
            Section15s = new List<int>(fxr.Section15s);
        }

        protected override bool Is(BinaryReaderEx br)
        {
            if (br.Length < 8L)
                return false;
            string ascii = br.GetASCII(0L, 4);
            short int16 = br.GetInt16(6L);
            return ascii == "FXR\0" && (int16 == 4 || int16 == 5);
        }

        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = false;

            br.AssertASCII("FXR\0");
            int num1 = br.AssertInt16(0);
            Version = br.ReadEnum16<FXRVersion>();
            br.AssertInt32(1);
            Id = br.ReadInt32();
            int stateMachineOffset = br.ReadInt32();
            br.AssertInt32(1); // Section 1 count
            br.ReadInt32(); // Section 2 offset
            br.ReadInt32(); // Section 2 count
            br.ReadInt32(); // Section 3 offset
            br.ReadInt32(); // Section 3 count
            int effectCallAOffset = br.ReadInt32();
            br.ReadInt32(); // Section 4 count
            br.ReadInt32(); // Section 5 offset
            br.ReadInt32(); // Section 5 count
            br.ReadInt32(); // Section 6 offset
            br.ReadInt32(); // Section 6 count
            br.ReadInt32(); // Section 7 offset
            br.ReadInt32(); // Section 7 count
            br.ReadInt32(); // Section 8 offset
            br.ReadInt32(); // Section 8 count
            br.ReadInt32(); // Section 9 offset
            br.ReadInt32(); // Section 9 count
            br.ReadInt32(); // Section 10 offset
            br.ReadInt32(); // Section 10 count
            br.ReadInt32(); // Section 11 offset
            br.ReadInt32(); // Section 11 count
            br.AssertInt32(1);
            br.AssertInt32(0);

            if (Version == FXRVersion.Sekiro)
            {
                int section12Offset = br.ReadInt32();
                int section12Count = br.ReadInt32();
                int section13Offset = br.ReadInt32();
                int section13Count = br.ReadInt32();
                int section14Offset = br.ReadInt32();
                int section14Count = br.ReadInt32();
                int section15Offset = br.ReadInt32();
                int section15Count = br.ReadInt32();
                // br.ReadInt32(); // Section 15 offset
                // br.AssertInt32(0); // Section 15 count

                Section12s = new List<int>(br.GetInt32s(section12Offset, section12Count));
                Section13s = new List<int>(br.GetInt32s(section13Offset, section13Count));
                Section14s = new List<int>(br.GetInt32s(section14Offset, section14Count));
                Section15s = new List<int>(br.GetInt32s(section15Offset, section15Count));
            }
            else
            {
                Section12s = new List<int>();
                Section13s = new List<int>();
                Section14s = new List<int>();
                Section15s = new List<int>();
            }

            br.Position = stateMachineOffset;
            RootStateMachine = new FFXStateMachine(br);

            br.Position = effectCallAOffset;
            RootEffectCall = new FFXEffectCallA(br);
        }

        protected override void Write(BinaryWriterEx bw)
        {
            bw.WriteASCII("FXR\0");
            bw.WriteInt16(0);
            bw.WriteUInt16((ushort)Version);
            bw.WriteInt32(1);
            bw.WriteInt32(Id);
            bw.ReserveInt32("StateMachineOffset");
            bw.WriteInt32(1);
            bw.ReserveInt32("FFXStateOffset");
            bw.WriteInt32(RootStateMachine.States.Count);
            bw.ReserveInt32("FFXTransitionOffset");
            bw.ReserveInt32("FFXTransitionCount");
            bw.ReserveInt32("EffectCallAOffset");
            bw.ReserveInt32("EffectCallACount");
            bw.ReserveInt32("EffectCallBOffset");
            bw.ReserveInt32("EffectCallBCount");
            bw.ReserveInt32("ActionCallOffset");
            bw.ReserveInt32("ActionCallCount");
            bw.ReserveInt32("FFXPropertyOffset");
            bw.ReserveInt32("FFXPropertyCount");
            bw.ReserveInt32("Section8Offset");
            bw.ReserveInt32("Section8Count");
            bw.ReserveInt32("Section9Offset");
            bw.ReserveInt32("Section9Count");
            bw.ReserveInt32("Section10Offset");
            bw.ReserveInt32("Section10Count");
            bw.ReserveInt32("FieldOffset");
            bw.ReserveInt32("FieldCount");
            bw.WriteInt32(1);
            bw.WriteInt32(0);

            if (Version == FXRVersion.Sekiro)
            {
                bw.ReserveInt32("Section12Offset");
                bw.WriteInt32(Section12s.Count);
                bw.ReserveInt32("Section13Offset");
                bw.WriteInt32(Section13s.Count);
                bw.ReserveInt32("Section14Offset");
                bw.WriteInt32(Section14s.Count);
                bw.ReserveInt32("Section15Offset");
                bw.WriteInt32(Section15s.Count);
                // bw.WriteInt32(0);
                // bw.WriteInt32(0);
            }

            bw.FillInt32("StateMachineOffset", (int)bw.Position);
            RootStateMachine.Write(bw);
            bw.Pad(16);
            bw.FillInt32("FFXStateOffset", (int)bw.Position);
            RootStateMachine.WriteStates(bw);
            bw.Pad(16);
            bw.FillInt32("FFXTransitionOffset", (int)bw.Position);
            List<FFXState> states = RootStateMachine.States;
            List<FFXTransition> ffxTransitions = new List<FFXTransition>();
            for (int index = 0; index < states.Count; ++index)
                states[index].WriteTransitions(bw, index, ffxTransitions);
            bw.FillInt32("FFXTransitionCount", ffxTransitions.Count);
            bw.Pad(16);
            bw.FillInt32("EffectCallAOffset", (int)bw.Position);
            List<FFXEffectCallA> effectCallAs = new List<FFXEffectCallA>();
            RootEffectCall.Write(bw, effectCallAs);
            RootEffectCall.WriteEffectCallAs(bw, effectCallAs);
            bw.FillInt32("EffectCallACount", effectCallAs.Count);
            bw.Pad(16);
            bw.FillInt32("EffectCallBOffset", (int)bw.Position);
            int effectCallBCount = 0;
            for (int index = 0; index < effectCallAs.Count; ++index)
                effectCallAs[index].WriteEffectCallBs(bw, index, ref effectCallBCount);
            bw.FillInt32("EffectCallBCount", effectCallBCount);
            bw.Pad(16);
            bw.FillInt32("ActionCallOffset", (int)bw.Position);
            effectCallBCount = 0;
            List<FFXActionCall> actionCalls = new List<FFXActionCall>();
            for (int index = 0; index < effectCallAs.Count; ++index)
                effectCallAs[index].WriteActionCalls(bw, index, ref effectCallBCount, actionCalls);
            bw.FillInt32("ActionCallCount", actionCalls.Count);
            bw.Pad(16);
            bw.FillInt32("FFXPropertyOffset", (int)bw.Position);
            List<FFXProperty> ffxProperties = new List<FFXProperty>();
            for (int index = 0; index < actionCalls.Count; ++index)
                actionCalls[index].WriteFFXProperties(bw, index, ffxProperties);
            bw.FillInt32("FFXPropertyCount", ffxProperties.Count);
            bw.Pad(16);
            bw.FillInt32("Section8Offset", (int)bw.Position);
            List<Section8> section8s = new List<Section8>();
            for (int index = 0; index < ffxProperties.Count; ++index)
                ffxProperties[index].WriteSection8s(bw, index, section8s);
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
            for (int index = 0; index < actionCalls.Count; ++index)
                actionCalls[index].WriteSection10s(bw, index, section10s);
            bw.FillInt32("Section10Count", section10s.Count);
            bw.Pad(16);
            bw.FillInt32("FieldOffset", (int)bw.Position);
            int fieldCount = 0;
            for (int index = 0; index < ffxTransitions.Count; ++index)
                ffxTransitions[index].WriteFields(bw, index, ref fieldCount);
            for (int index = 0; index < actionCalls.Count; ++index)
                actionCalls[index].WriteFields(bw, index, ref fieldCount);
            for (int index = 0; index < ffxProperties.Count; ++index)
                ffxProperties[index].WriteFields(bw, index, ref fieldCount);
            for (int index = 0; index < section8s.Count; ++index)
                section8s[index].WriteFields(bw, index, ref fieldCount);
            for (int index = 0; index < section9s.Count; ++index)
                section9s[index].WriteFields(bw, index, ref fieldCount);
            for (int index = 0; index < section10s.Count; ++index)
                section10s[index].WriteFields(bw, index, ref fieldCount);
            bw.FillInt32("FieldCount", fieldCount);
            bw.Pad(16);

            if (Version != FXRVersion.Sekiro)
                return;

            bw.FillInt32("Section12Offset", (int)bw.Position);
            bw.WriteInt32s(Section12s);
            bw.Pad(16);

            bw.FillInt32("Section13Offset", (int)bw.Position);
            bw.WriteInt32s(Section13s);
            bw.Pad(16);

            bw.FillInt32("Section14Offset", (int)bw.Position);
            bw.WriteInt32s(Section14s);
            bw.Pad(16);

            if (Section15s.Count > 0)
            {
                bw.FillInt32("Section15Offset", (int)bw.Position);
                bw.WriteInt32s(Section15s);
                bw.Pad(16);
            }
            else
            {
                bw.FillInt32("Section15Offset", 0);
            }
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
                br.AssertInt32(0);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(num);
                States = new List<FFXState>(capacity);
                for (int index = 0; index < capacity; ++index)
                    States.Add(new FFXState(br));
                br.StepOut();
            }

            internal FFXStateMachine(FFXStateMachine stateMachine)
            {
                States = stateMachine.States.Select(state => new FFXState(state)).ToList();
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(States.Count);
                bw.ReserveInt32("Section1StatesOffset");
                bw.WriteInt32(0);
            }

            internal void WriteStates(BinaryWriterEx bw)
            {
                bw.FillInt32("Section1StatesOffset", (int)bw.Position);
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
                br.AssertInt32(0);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(num);
                Transitions = new List<FFXTransition>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Transitions.Add(new FFXTransition(br));
                br.StepOut();
            }

            internal FFXState(FFXState state)
            {
                Transitions = state.Transitions.Select(transition => new FFXTransition(transition)).ToList();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt32(0);
                bw.WriteInt32(Transitions.Count);
                bw.ReserveInt32(string.Format("StateTransitionsOffset[{0}]", index));
                bw.WriteInt32(0);
            }

            internal void WriteTransitions(
                BinaryWriterEx bw,
                int index,
                List<FFXTransition> ffxTransitions)
            {
                bw.FillInt32(string.Format("StateTransitionsOffset[{0}]", index), (int)bw.Position);
                foreach (FFXTransition ffxTransition in Transitions)
                    ffxTransition.Write(bw, ffxTransitions);
            }
        }

        public class FFXTransition
        {
            public short Unk00 { get; set; }

            [XmlAttribute] public int TargetStateIndex { get; set; }

            public int Unk10 { get; set; }

            public int Unk38 { get; set; }

            public int Unk40 { get; set; }

            public int FieldData1 { get; set; }

            public int FieldData2 { get; set; }

            public FFXTransition()
            {
            }

            internal FFXTransition(BinaryReaderEx br)
            {
                Unk00 = br.AssertInt16(8, 10, 11);
                int num2 = br.AssertByte(0);
                int num3 = br.AssertByte(1);
                br.AssertInt32(0);
                TargetStateIndex = br.ReadInt32();
                br.AssertInt32(0);
                Unk10 = br.AssertInt32(16842748, 16842749, 16842750, 16842751);
                br.AssertInt32(0);
                br.AssertInt32(1);
                br.AssertInt32(0);
                int fieldOffset1 = br.ReadInt32();
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                Unk38 = br.AssertInt32(16842748, 16842749, 16842750, 16842751);
                br.AssertInt32(0);
                Unk40 = br.AssertInt32(0, 1);
                br.AssertInt32(0);
                int fieldOffset2 = br.ReadInt32();
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                FieldData1 = br.GetInt32(fieldOffset1);
                FieldData2 = br.GetInt32(fieldOffset2);
            }

            internal FFXTransition(FFXTransition transition)
            {
                Unk00 = transition.Unk00;
                TargetStateIndex = transition.TargetStateIndex;
                Unk10 = transition.Unk10;
                Unk38 = transition.Unk38;
                Unk40 = transition.Unk40;
                FieldData1 = transition.FieldData1;
                FieldData2 = transition.FieldData2;
            }

            internal void Write(BinaryWriterEx bw, List<FFXTransition> ffxTransitions)
            {
                int count = ffxTransitions.Count;
                bw.WriteInt16(Unk00);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(0);
                bw.WriteInt32(TargetStateIndex);
                bw.WriteInt32(0);
                bw.WriteInt32(Unk10);
                bw.WriteInt32(0);
                bw.WriteInt32(1);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("TransitionFieldOffset1[{0}]", count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(Unk38);
                bw.WriteInt32(0);
                bw.WriteInt32(Unk40);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("TransitionFieldOffset2[{0}]", count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                ffxTransitions.Add(this);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                bw.FillInt32(string.Format("TransitionFieldOffset1[{0}]", index), (int)bw.Position);
                bw.WriteInt32(FieldData1);
                bw.FillInt32(string.Format("TransitionFieldOffset2[{0}]", index), (int)bw.Position);
                bw.WriteInt32(FieldData2);
                fieldCount += 2;
            }
        }

        public class FFXEffectCallA
        {
            [XmlAttribute] public short EffectID { get; set; }

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
                int num1 = br.AssertByte(0);
                int num2 = br.AssertByte(1);
                br.AssertInt32(0);
                int effectCallBCount = br.ReadInt32();
                int actionCallCount = br.ReadInt32();
                int effectCallACount = br.ReadInt32();
                br.AssertInt32(0);
                int effectCallBOffset = br.ReadInt32();
                br.AssertInt32(0);
                int actionCallOffset = br.ReadInt32();
                br.AssertInt32(0);
                int effectCallAOffset = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(effectCallAOffset);
                EffectAs = new List<FFXEffectCallA>(effectCallACount);
                for (int index = 0; index < effectCallACount; ++index)
                    EffectAs.Add(new FFXEffectCallA(br));
                br.StepOut();
                br.StepIn(effectCallBOffset);
                EffectBs = new List<FFXEffectCallB>(effectCallBCount);
                for (int index = 0; index < effectCallBCount; ++index)
                    EffectBs.Add(new FFXEffectCallB(br));
                br.StepOut();
                br.StepIn(actionCallOffset);
                Actions = new List<FFXActionCall>(actionCallCount);
                for (int index = 0; index < actionCallCount; ++index)
                    Actions.Add(new FFXActionCall(br));
                br.StepOut();
            }

            internal FFXEffectCallA(FFXEffectCallA effectCallA)
            {
                EffectID = effectCallA.EffectID;
                EffectAs = effectCallA.EffectAs.Select(effectA => new FFXEffectCallA(effectA)).ToList();
                EffectBs = effectCallA.EffectBs.Select(effectB => new FFXEffectCallB(effectB)).ToList();
                Actions = effectCallA.Actions.Select(action => new FFXActionCall(action)).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<FFXEffectCallA> effectCallAs)
            {
                int count = effectCallAs.Count;
                bw.WriteInt16(EffectID);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(0);
                bw.WriteInt32(EffectBs.Count);
                bw.WriteInt32(Actions.Count);
                bw.WriteInt32(EffectAs.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("EffectCallAEffectCallBsOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("EffectCallAActionCallsOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("EffectCallAEffectCallAsOffset[{0}]", count));
                bw.WriteInt32(0);
                effectCallAs.Add(this);
            }

            internal void WriteEffectCallAs(BinaryWriterEx bw, List<FFXEffectCallA> effectCallAs)
            {
                int num = effectCallAs.IndexOf(this);
                if (EffectAs.Count == 0)
                {
                    bw.FillInt32(string.Format("EffectCallAEffectCallAsOffset[{0}]", num), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("EffectCallAEffectCallAsOffset[{0}]", num), (int)bw.Position);
                    foreach (FFXEffectCallA effectA in EffectAs)
                        effectA.Write(bw, effectCallAs);
                    foreach (FFXEffectCallA effectA in EffectAs)
                        effectA.WriteEffectCallAs(bw, effectCallAs);
                }
            }

            internal void WriteEffectCallBs(BinaryWriterEx bw, int index, ref int effectCallBCount)
            {
                if (EffectBs.Count == 0)
                {
                    bw.FillInt32(string.Format("EffectCallAEffectCallBsOffset[{0}]", index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("EffectCallAEffectCallBsOffset[{0}]", index), (int)bw.Position);
                    for (int index1 = 0; index1 < EffectBs.Count; ++index1)
                        EffectBs[index1].Write(bw, effectCallBCount + index1);
                    effectCallBCount += EffectBs.Count;
                }
            }

            internal void WriteActionCalls(
                BinaryWriterEx bw,
                int index,
                ref int effectCallBCount,
                List<FFXActionCall> actionCalls)
            {
                bw.FillInt32(string.Format("EffectCallAActionCallsOffset[{0}]", index), (int)bw.Position);
                foreach (FFXActionCall action in Actions)
                    action.Write(bw, actionCalls);
                for (int index1 = 0; index1 < EffectBs.Count; ++index1)
                    EffectBs[index1].WriteActionCalls(bw, effectCallBCount + index1, actionCalls);
                effectCallBCount += EffectBs.Count;
            }
        }

        public class FFXEffectCallB
        {
            [XmlAttribute] public short EffectID { get; set; }

            public List<FFXActionCall> Actions { get; set; }

            public FFXEffectCallB() => Actions = new List<FFXActionCall>();

            internal FFXEffectCallB(BinaryReaderEx br)
            {
                EffectID = br.ReadInt16();
                int num1 = br.AssertByte(0);
                int num2 = br.AssertByte(1);
                br.AssertInt32(0);
                br.AssertInt32(0);
                int capacity = br.ReadInt32();
                br.AssertInt32(0);
                br.AssertInt32(0);
                int num3 = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(num3);
                Actions = new List<FFXActionCall>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Actions.Add(new FFXActionCall(br));
                br.StepOut();
            }

            internal FFXEffectCallB(FFXEffectCallB effectCallB)
            {
                EffectID = effectCallB.EffectID;
                Actions = effectCallB.Actions.Select(action => new FFXActionCall(action)).ToList();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt16(EffectID);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(Actions.Count);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("EffectCallBActionCallsOffset[{0}]", index));
                bw.WriteInt32(0);
            }

            internal void WriteActionCalls(
                BinaryWriterEx bw,
                int index,
                List<FFXActionCall> actionCalls)
            {
                bw.FillInt32(string.Format("EffectCallBActionCallsOffset[{0}]", index), (int)bw.Position);
                foreach (FFXActionCall action in Actions)
                    action.Write(bw, actionCalls);
            }
        }

        public class FFXActionCall
        {
            [XmlAttribute] public short ActionID { get; set; }

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
                ActionID = br.ReadInt16(); // 0
                Unk02 = br.ReadBoolean(); // 2
                Unk03 = br.ReadBoolean(); // 3
                Unk04 = br.ReadInt32(); // 4
                int fieldCount1 = br.ReadInt32(); // 8
                int section10Count = br.ReadInt32(); // 12
                int propertyCount1 = br.ReadInt32(); // 16
                int fieldCount2 = br.ReadInt32(); // 20
                br.AssertInt32(0);
                int propertyCount2 = br.ReadInt32();
                int fieldOffset = br.ReadInt32();
                // Console.WriteLine($"fieldOffset: {fieldOffset}");
                br.AssertInt32(0);
                int section10Offset = br.ReadInt32();
                br.AssertInt32(0);
                int propertyOffset = br.ReadInt32();
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);

                br.StepIn(propertyOffset);
                {
                    Properties1 = new List<FFXProperty>(propertyCount1);
                    for (int index = 0; index < propertyCount1; ++index)
                        Properties1.Add(new FFXProperty(br));

                    Properties2 = new List<FFXProperty>(propertyCount2);
                    for (int index = 0; index < propertyCount2; ++index)
                        Properties2.Add(new FFXProperty(br));
                }
                br.StepOut();

                br.StepIn(section10Offset);
                {
                    Section10s = new List<Section10>(section10Count);
                    for (int index = 0; index < section10Count; ++index)
                        Section10s.Add(new Section10(br));
                }
                br.StepOut();

                br.StepIn(fieldOffset);
                {
                    Fields1 = FFXField.ReadMany(br, fieldCount1);
                    Fields2 = FFXField.ReadMany(br, fieldCount2);
                }
                br.StepOut();
            }

            internal FFXActionCall(FFXActionCall action)
            {
                ActionID = action.ActionID;
                Unk02 = action.Unk02;
                Unk03 = action.Unk03;
                Unk04 = action.Unk04;
                Properties1 = action.Properties1.Select(prop => new FFXProperty(prop)).ToList();
                Properties2 = action.Properties2.Select(prop => new FFXProperty(prop)).ToList();
                Section10s = action.Section10s.Select(section => new Section10(section)).ToList();
                Fields1 = action.Fields1.Select(FFXField.Create).ToList();
                Fields2 = action.Fields2.Select(FFXField.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<FFXActionCall> actionCalls)
            {
                int count = actionCalls.Count;
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
                bw.ReserveInt32($"ActionCallFieldsOffset[{count}]");
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("ActionCallSection10sOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("ActionCallFFXPropertiesOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                actionCalls.Add(this);
            }

            internal void WriteFFXProperties(BinaryWriterEx bw, int index, List<FFXProperty> ffxProperties)
            {
                bw.FillInt32(string.Format("ActionCallFFXPropertiesOffset[{0}]", index), (int)bw.Position);
                foreach (FFXProperty ffxProperty in Properties1)
                    ffxProperty.Write(bw, ffxProperties);
                foreach (FFXProperty ffxProperty in Properties2)
                    ffxProperty.Write(bw, ffxProperties);
            }

            internal void WriteSection10s(BinaryWriterEx bw, int index, List<Section10> section10s)
            {
                bw.FillInt32(string.Format("ActionCallSection10sOffset[{0}]", index), (int)bw.Position);
                foreach (Section10 section10 in Section10s)
                    section10.Write(bw, section10s);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                if (Fields1.Count == 0 && Fields2.Count == 0)
                {
                    bw.FillInt32(string.Format("ActionCallFieldsOffset[{0}]", index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("ActionCallFieldsOffset[{0}]", index), (int)bw.Position);
                    foreach (FFXField ffxField in Fields1)
                        ffxField.Write(bw);
                    foreach (FFXField ffxField in Fields2)
                        ffxField.Write(bw);
                    fieldCount += Fields1.Count + Fields2.Count;
                }
            }
        }

        [XmlInclude(typeof(FFXFieldFloat))]
        [XmlInclude(typeof(FFXFieldInt))]
        public abstract class FFXField
        {
            public static FFXField Create(float value)
            {
                return new FFXFieldFloat(value);
            }

            public static FFXField Create(int value)
            {
                return new FFXFieldInt(value);
            }

            public static FFXField Create(FFXField field)
            {
                if (field.GetType() == typeof(FFXFieldFloat))
                {
                    return new FFXFieldFloat(((FFXFieldFloat) field).Value);
                }

                if (field.GetType() == typeof(FFXFieldInt))
                {
                    return new FFXFieldInt(((FFXFieldInt) field).Value);
                }

                throw new InvalidOperationException("Field passed for creation was neither Float nor Int");
            }

            public static FFXFieldFloat Create(FFXFieldFloat field)
            {
                return new FFXFieldFloat(field.Value);
            }

            public static FFXFieldInt Create(FFXFieldInt field)
            {
                return new FFXFieldInt(field.Value);
            }
            public static FFXField Read(BinaryReaderEx br)
            {
                float single = br.GetSingle(br.Position);
                FFXField ffxField;
                if (single >= 9.99999974737875E-05 && single < 1000000.0 ||
                    single <= -9.99999974737875E-05 && single > -1000000.0)
                    ffxField = new FFXFieldFloat(single);
                else
                    ffxField = new FFXFieldInt(br.GetInt32(br.Position));
                br.Position += 4L;
                return ffxField;
            }

            public static List<FFXField> ReadMany(BinaryReaderEx br, int count)
            {
                List<FFXField> ffxFieldList = new List<FFXField>();
                for (int index = 0; index < count; ++index)
                    ffxFieldList.Add(Read(br));
                return ffxFieldList;
            }

            public static List<FFXField> ReadManyAt(
                BinaryReaderEx br,
                int offset,
                int count)
            {
                br.StepIn(offset);
                List<FFXField> ffxFieldList = ReadMany(br, count);
                br.StepOut();
                return ffxFieldList;
            }

            public abstract void Write(BinaryWriterEx bw);

            public class FFXFieldFloat : FFXField
            {
                [XmlAttribute] public float Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteSingle(Value);

                public FFXFieldFloat()
                {
                    Value = 0;
                }
                public FFXFieldFloat(float value)
                {
                    Value = value;
                }
            }

            public class FFXFieldInt : FFXField
            {
                [XmlAttribute] public int Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteInt32(Value);

                public FFXFieldInt()
                {
                    Value = 0;
                }
                public FFXFieldInt(int value)
                {
                    Value = value;
                }
            }
        }

        public class FFXProperty
        {
            [XmlAttribute] public short TypeEnumA { get; set; }

            [XmlAttribute] public int TypeEnumB { get; set; }

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
                int num1 = br.AssertByte(0);
                int num2 = br.AssertByte(1);
                TypeEnumB = br.ReadInt32();
                int count = br.ReadInt32();
                br.AssertInt32(0);
                int offset = br.ReadInt32();
                br.AssertInt32(0);
                int num3 = br.ReadInt32();
                br.AssertInt32(0);
                int capacity = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(num3);
                Section8s = new List<Section8>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Section8s.Add(new Section8(br));
                br.StepOut();
                Fields = new List<FFXField>();
                Fields = FFXField.ReadManyAt(br, offset, count);
            }

            internal FFXProperty(FFXProperty prop)
            {
                TypeEnumA = prop.TypeEnumA;
                TypeEnumB = prop.TypeEnumB;
                Section8s = prop.Section8s.Select(section => new Section8(section)).ToList();
                Fields = prop.Fields.Select(FFXField.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<FFXProperty> ffxProperties)
            {
                int count = ffxProperties.Count;
                bw.WriteInt16(TypeEnumA);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(TypeEnumB);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("FFXPropertyFieldsOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("FFXPropertySection8sOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.WriteInt32(Section8s.Count);
                bw.WriteInt32(0);
                ffxProperties.Add(this);
            }

            internal void WriteSection8s(BinaryWriterEx bw, int index, List<Section8> section8s)
            {
                bw.FillInt32(string.Format("FFXPropertySection8sOffset[{0}]", index), (int)bw.Position);
                foreach (Section8 section8 in Section8s)
                    section8.Write(bw, section8s);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                if (Fields.Count == 0)
                {
                    bw.FillInt32(string.Format("FFXPropertyFieldsOffset[{0}]", index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("FFXPropertyFieldsOffset[{0}]", index), (int)bw.Position);
                    foreach (FFXField field in Fields)
                        field.Write(bw);
                    fieldCount += Fields.Count;
                }
            }
        }

        public class Section8
        {
            [XmlAttribute] public ushort Unk00 { get; set; }

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
                int num1 = br.AssertByte(0);
                int num2 = br.AssertByte(1);
                Unk04 = br.ReadInt32();
                int count = br.ReadInt32();
                int capacity = br.ReadInt32();
                int offset = br.ReadInt32();
                br.AssertInt32(0);
                int num3 = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(num3);
                Section9s = new List<Section9>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Section9s.Add(new Section9(br));
                br.StepOut();
                Fields = FFXField.ReadManyAt(br, offset, count);
            }

            internal Section8(Section8 section)
            {
                Unk00 = section.Unk00;
                Unk04 = section.Unk04;
                Section9s = section.Section9s.Select(section => new Section9(section)).ToList();
                Fields = section.Fields.Select(FFXField.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<Section8> section8s)
            {
                int count = section8s.Count;
                bw.WriteUInt16(Unk00);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(Unk04);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(Section9s.Count);
                bw.ReserveInt32(string.Format("Section8FieldsOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section8Section9sOffset[{0}]", count));
                bw.WriteInt32(0);
                section8s.Add(this);
            }

            internal void WriteSection9s(BinaryWriterEx bw, int index, List<Section9> section9s)
            {
                bw.FillInt32(string.Format("Section8Section9sOffset[{0}]", index), (int)bw.Position);
                foreach (Section9 section9 in Section9s)
                    section9.Write(bw, section9s);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                bw.FillInt32(string.Format("Section8FieldsOffset[{0}]", index), (int)bw.Position);
                foreach (FFXField field in Fields)
                    field.Write(bw);
                fieldCount += Fields.Count;
            }
        }

        public class Section9
        {
            public short Unk00 { get; set; }
            public int Unk04 { get; set; }

            public List<FFXField> Fields { get; set; }

            public Section9() => Fields = new List<FFXField>();

            internal Section9(BinaryReaderEx br)
            {
                Unk00 = br.AssertInt16(48, 64, 67);
                int num2 = br.AssertByte(0);
                int num3 = br.AssertByte(1);
                Unk04 = br.ReadInt32();
                int count = br.ReadInt32();
                br.AssertInt32(0);
                int offset = br.ReadInt32();
                br.AssertInt32(0);
                Fields = FFXField.ReadManyAt(br, offset, count);
            }

            internal Section9(Section9 section)
            {
                Unk00 = section.Unk00;
                Unk04 = section.Unk04;
                Fields = section.Fields.Select(FFXField.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<Section9> section9s)
            {
                int count = section9s.Count;
                bw.WriteInt16(Unk00);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(Unk04);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section9FieldsOffset[{0}]", count));
                bw.WriteInt32(0);
                section9s.Add(this);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                bw.FillInt32(string.Format("Section9FieldsOffset[{0}]", index), (int)bw.Position);
                foreach (FFXField field in Fields)
                    field.Write(bw);
                fieldCount += Fields.Count;
            }
        }

        public class Section10
        {
            public List<FFXField> Fields { get; set; }

            public Section10() => Fields = new List<FFXField>();

            internal Section10(BinaryReaderEx br)
            {
                int offset = br.ReadInt32();
                br.AssertInt32(0);
                int count = br.ReadInt32();
                br.AssertInt32(0);
                Fields = FFXField.ReadManyAt(br, offset, count);
            }

            internal Section10(Section10 section)
            {
                Fields = section.Fields.Select(FFXField.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<Section10> section10s)
            {
                int count = section10s.Count;
                bw.ReserveInt32(string.Format("Section10FieldsOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(0);
                section10s.Add(this);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                bw.FillInt32(string.Format("Section10FieldsOffset[{0}]", index), (int)bw.Position);
                foreach (FFXField field in Fields)
                    field.Write(bw);
                fieldCount += Fields.Count;
            }
        }
    }

    public class FXR3EnhancedSerialization
    {
        public static Fxr3 XMLToFXR3(XDocument XML)
        {
            XmlSerializer test = new XmlSerializer(typeof(Fxr3));
            XmlReader xmlReader = XML.CreateReader();

            return (Fxr3)test.Deserialize(xmlReader);
        }

        public static XDocument FXR3ToXML(Fxr3 fxr)
        {
            XDocument XDoc = new XDocument();

            using (var xmlWriter = XDoc.CreateWriter())
            {
                var thing = new XmlSerializer(typeof(Fxr3));
                thing.Serialize(xmlWriter, fxr);
            }

            return XDoc;
        }
    }
}