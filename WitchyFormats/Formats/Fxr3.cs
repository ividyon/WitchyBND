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
    /// Initial work by TKGP, Meowmaritus and NamelessHoodie.
    /// Currently maintained by ivi.
    /// </summary>
    [XmlType(TypeName="fxr3")]
    public class Fxr3 : SoulsFile<Fxr3>
    {
        public FXRVersion Version { get; set; }

        public int Id { get; set; }

        public StateMap RootStateMap { get; set; }

        public Container RootContainer { get; set; }

        public List<int> References { get; set; }

        public List<int> UnkExternalValues { get; set; }

        public List<int> UnkBloodEnabler { get; set; }

        public List<int> UnkEmpty { get; set; }

        public Fxr3()
        {
            Version = FXRVersion.DarkSouls3;
            RootStateMap = new StateMap();
            RootContainer = new Container();
            References = new List<int>();
            UnkExternalValues = new List<int>();
            UnkBloodEnabler = new List<int>();
            UnkEmpty = new List<int>();
        }

        public Fxr3(Fxr3 fxr)
        {
            Id = fxr.Id;
            Version = fxr.Version;
            RootStateMap = new StateMap(fxr.RootStateMap);
            RootContainer = new Container(fxr.RootContainer);
            References = new List<int>(fxr.References);
            UnkExternalValues = new List<int>(fxr.UnkExternalValues);
            UnkBloodEnabler = new List<int>(fxr.UnkBloodEnabler);
            UnkEmpty = new List<int>(fxr.UnkEmpty);
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
            int stateMapOffset = br.ReadInt32();
            br.AssertInt32(1); // Section 1 count
            br.ReadInt32(); // Section 2 offset
            br.ReadInt32(); // Section 2 count
            br.ReadInt32(); // Section 3 offset
            br.ReadInt32(); // Section 3 count
            int containerOffset = br.ReadInt32();
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

                References = new List<int>(br.GetInt32s(section12Offset, section12Count));
                UnkExternalValues = new List<int>(br.GetInt32s(section13Offset, section13Count));
                UnkBloodEnabler = new List<int>(br.GetInt32s(section14Offset, section14Count));
                UnkEmpty = new List<int>(br.GetInt32s(section15Offset, section15Count));
            }
            else
            {
                References = new List<int>();
                UnkExternalValues = new List<int>();
                UnkBloodEnabler = new List<int>();
                UnkEmpty = new List<int>();
            }

            br.Position = stateMapOffset;
            RootStateMap = new StateMap(br);

            br.Position = containerOffset;
            RootContainer = new Container(br);
        }

        protected override void Write(BinaryWriterEx bw)
        {
            bw.WriteASCII("FXR\0");
            bw.WriteInt16(0);
            bw.WriteUInt16((ushort)Version);
            bw.WriteInt32(1);
            bw.WriteInt32(Id);
            bw.ReserveInt32("StateMapOffset");
            bw.WriteInt32(1);
            bw.ReserveInt32("StateOffset");
            bw.WriteInt32(RootStateMap.States.Count);
            bw.ReserveInt32("TransitionOffset");
            bw.ReserveInt32("TransitionCount");
            bw.ReserveInt32("ContainerOffset");
            bw.ReserveInt32("ContainerCount");
            bw.ReserveInt32("EffectOffset");
            bw.ReserveInt32("EffectCount");
            bw.ReserveInt32("ActionOffset");
            bw.ReserveInt32("ActionCount");
            bw.ReserveInt32("PropertyOffset");
            bw.ReserveInt32("PropertyCount");
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
                bw.WriteInt32(References.Count);
                bw.ReserveInt32("Section13Offset");
                bw.WriteInt32(UnkExternalValues.Count);
                bw.ReserveInt32("Section14Offset");
                bw.WriteInt32(UnkBloodEnabler.Count);
                bw.ReserveInt32("Section15Offset");
                bw.WriteInt32(UnkEmpty.Count);
                // bw.WriteInt32(0);
                // bw.WriteInt32(0);
            }

            bw.FillInt32("StateMapOffset", (int)bw.Position);
            RootStateMap.Write(bw);
            bw.Pad(16);
            bw.FillInt32("StateOffset", (int)bw.Position);
            RootStateMap.WriteStates(bw);
            bw.Pad(16);
            bw.FillInt32("TransitionOffset", (int)bw.Position);
            List<State> states = RootStateMap.States;
            List<Transition> transitions = new List<Transition>();
            for (int index = 0; index < states.Count; ++index)
                states[index].WriteTransitions(bw, index, transitions);
            bw.FillInt32("TransitionCount", transitions.Count);
            bw.Pad(16);
            bw.FillInt32("ContainerOffset", (int)bw.Position);
            List<Container> Containers = new List<Container>();
            RootContainer.Write(bw, Containers);
            RootContainer.WriteContainers(bw, Containers);
            bw.FillInt32("ContainerCount", Containers.Count);
            bw.Pad(16);
            bw.FillInt32("EffectOffset", (int)bw.Position);
            int EffectCount = 0;
            for (int index = 0; index < Containers.Count; ++index)
                Containers[index].WriteEffects(bw, index, ref EffectCount);
            bw.FillInt32("EffectCount", EffectCount);
            bw.Pad(16);
            bw.FillInt32("ActionOffset", (int)bw.Position);
            EffectCount = 0;
            List<Action> actions = new List<Action>();
            for (int index = 0; index < Containers.Count; ++index)
                Containers[index].WriteActions(bw, index, ref EffectCount, actions);
            bw.FillInt32("ActionCount", actions.Count);
            bw.Pad(16);
            bw.FillInt32("PropertyOffset", (int)bw.Position);
            List<Property> properties = new List<Property>();
            for (int index = 0; index < actions.Count; ++index)
                actions[index].WriteProperties(bw, index, properties);
            bw.FillInt32("PropertyCount", properties.Count);
            bw.Pad(16);
            bw.FillInt32("Section8Offset", (int)bw.Position);
            List<PropertyModifier> modifiers = new List<PropertyModifier>();
            for (int index = 0; index < properties.Count; ++index)
                properties[index].WriteModifiers(bw, index, modifiers);
            bw.FillInt32("Section8Count", modifiers.Count);
            bw.Pad(16);
            bw.FillInt32("Section9Offset", (int)bw.Position);
            List<Property> conditionalProperties = new List<Property>();
            for (int index = 0; index < modifiers.Count; ++index)
                modifiers[index].WriteProperties(bw, index, conditionalProperties);
            bw.FillInt32("Section9Count", conditionalProperties.Count);
            bw.Pad(16);
            bw.FillInt32("Section10Offset", (int)bw.Position);
            List<Section10> section10s = new List<Section10>();
            for (int index = 0; index < actions.Count; ++index)
                actions[index].WriteSection10s(bw, index, section10s);
            bw.FillInt32("Section10Count", section10s.Count);
            bw.Pad(16);
            bw.FillInt32("FieldOffset", (int)bw.Position);
            int fieldCount = 0;
            for (int index = 0; index < transitions.Count; ++index)
                transitions[index].WriteFields(bw, index, ref fieldCount);
            for (int index = 0; index < actions.Count; ++index)
                actions[index].WriteFields(bw, index, ref fieldCount);
            for (int index = 0; index < properties.Count; ++index)
                properties[index].WriteFields(bw, index, ref fieldCount, false);
            for (int index = 0; index < modifiers.Count; ++index)
                modifiers[index].WriteFields(bw, index, ref fieldCount);
            for (int index = 0; index < conditionalProperties.Count; ++index)
                conditionalProperties[index].WriteFields(bw, index, ref fieldCount, true);
            for (int index = 0; index < section10s.Count; ++index)
                section10s[index].WriteFields(bw, index, ref fieldCount);
            bw.FillInt32("FieldCount", fieldCount);
            bw.Pad(16);

            if (Version != FXRVersion.Sekiro)
                return;

            bw.FillInt32("Section12Offset", (int)bw.Position);
            bw.WriteInt32s(References);
            bw.Pad(16);

            bw.FillInt32("Section13Offset", (int)bw.Position);
            bw.WriteInt32s(UnkExternalValues);
            bw.Pad(16);

            bw.FillInt32("Section14Offset", (int)bw.Position);
            bw.WriteInt32s(UnkBloodEnabler);
            bw.Pad(16);

            if (UnkEmpty.Count > 0)
            {
                bw.FillInt32("Section15Offset", (int)bw.Position);
                bw.WriteInt32s(UnkEmpty);
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

        public class StateMap
        {
            public List<State> States { get; set; }

            public StateMap() => States = new List<State>();

            internal StateMap(BinaryReaderEx br)
            {
                br.AssertInt32(0);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(num);
                States = new List<State>(capacity);
                for (int index = 0; index < capacity; ++index)
                    States.Add(new State(br));
                br.StepOut();
            }

            internal StateMap(StateMap stateMap)
            {
                States = stateMap.States.Select(state => new State(state)).ToList();
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

        public class State
        {
            public List<Transition> Transitions { get; set; }

            public State() => Transitions = new List<Transition>();

            internal State(BinaryReaderEx br)
            {
                br.AssertInt32(0);
                int capacity = br.ReadInt32();
                int num = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(num);
                Transitions = new List<Transition>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Transitions.Add(new Transition(br));
                br.StepOut();
            }

            internal State(State state)
            {
                Transitions = state.Transitions.Select(transition => new Transition(transition)).ToList();
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
                List<Transition> transitions)
            {
                bw.FillInt32(string.Format("StateTransitionsOffset[{0}]", index), (int)bw.Position);
                foreach (Transition transition in Transitions)
                    transition.Write(bw, transitions);
            }
        }

        public class Transition
        {
            [XmlAttribute] public int TargetStateIndex { get; set; }

            public enum TransitionOperatorType
            {
                Equal = 0,
                NotEqual = 1,
                LessThan = 2,
                LessThanOrEqual = 3,
            }

            public TransitionOperatorType Operator { get; set; }

            public byte UnkOperatorModifier { get; set; }

            public enum TransitionFieldType
            {
                Literal = -4,
                External = -3,
                UnkMinus2 = -2,
                Age = -1
            }

            public TransitionFieldType LeftOperandType { get; set; }
            public Field? LeftOperand { get; set; }

            public TransitionFieldType RightOperandType { get; set; }
            public Field? RightOperand { get; set; }

            public Transition()
            {
            }

            internal Transition(BinaryReaderEx br)
            {
                var op = br.ReadInt16();
                Operator = (TransitionOperatorType)(op & 0b0011);
                UnkOperatorModifier = (byte)(op >> 2 & 0b0011);
                br.AssertByte(0);
                br.AssertByte(1);
                br.AssertInt32(0);
                TargetStateIndex = br.ReadInt32();
                br.AssertInt32(0);
                var field1Source = br.AssertInt16(-4, -3, -2, -1);
                LeftOperandType = (TransitionFieldType)field1Source;
                br.AssertByte(0);
                br.AssertByte(1);
                br.AssertInt32(0);
                bool hasField1 = br.AssertInt32(0, 1) == 1; // Has Field1 or not
                br.AssertInt32(0);
                int fieldOffset1 = br.ReadInt32();
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                var field2Source = br.AssertInt16(-4, -3, -2, -1);
                RightOperandType = (TransitionFieldType)field2Source;
                br.AssertByte(0);
                br.AssertByte(1);
                br.AssertInt32(0);
                bool hasField2 = br.AssertInt32(0, 1) == 1; // Has Field2 or not
                br.AssertInt32(0);
                int fieldOffset2 = br.ReadInt32();
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                br.AssertInt32(0);
                LeftOperand = hasField1 ? Field.ReadAt(br, fieldOffset1, this, 0) : null;
                RightOperand = hasField2 ? Field.ReadAt(br, fieldOffset2, this, 1) : null;
            }

            internal Transition(Transition transition)
            {
                Operator = transition.Operator;
                UnkOperatorModifier = transition.UnkOperatorModifier;
                TargetStateIndex = transition.TargetStateIndex;
                LeftOperandType = transition.LeftOperandType;
                RightOperandType = transition.RightOperandType;
                LeftOperand = transition.LeftOperand;
                RightOperand = transition.RightOperand;
            }

            internal void Write(BinaryWriterEx bw, List<Transition> transitions)
            {
                int count = transitions.Count;
                short op = (short)((byte)Operator | ((UnkOperatorModifier << 2) & 0b1100));
                bw.WriteInt16(op);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(0);
                bw.WriteInt32(TargetStateIndex);
                bw.WriteInt32(0);
                bw.WriteInt16((short)LeftOperandType);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(0);
                bw.WriteInt32(LeftOperand != null ? 1 : 0);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("TransitionFieldOffset1[{0}]", count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt16((short)RightOperandType);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(0);
                bw.WriteInt32(RightOperand != null ? 1 : 0);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("TransitionFieldOffset2[{0}]", count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                transitions.Add(this);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                int fieldPos1 = LeftOperand != null ? (int)bw.Position : 0;
                bw.FillInt32(string.Format("TransitionFieldOffset1[{0}]", index), fieldPos1);
                if (LeftOperand != null)
                {
                    LeftOperand.Write(bw);
                    fieldCount++;
                }

                int fieldPos2 = RightOperand != null ? (int)bw.Position : 0;
                bw.FillInt32(string.Format("TransitionFieldOffset2[{0}]", index), fieldPos2);
                if (RightOperand != null)
                {
                    RightOperand.Write(bw);
                    fieldCount++;
                }
            }
        }

        public class Container
        {
            [XmlAttribute] public short Id { get; set; }

            public List<Action> Actions { get; set; }
            public List<Effect> Effects { get; set; }
            public List<Container> Containers { get; set; }

            public Container()
            {
                Containers = new List<Container>();
                Effects = new List<Effect>();
                Actions = new List<Action>();
            }

            internal Container(BinaryReaderEx br)
            {
                Id = br.ReadInt16();
                int num1 = br.AssertByte(0);
                int num2 = br.AssertByte(1);
                br.AssertInt32(0);
                int EffectCount = br.ReadInt32();
                int ActionCount = br.ReadInt32();
                int ContainerCount = br.ReadInt32();
                br.AssertInt32(0);
                int EffectOffset = br.ReadInt32();
                br.AssertInt32(0);
                int ActionOffset = br.ReadInt32();
                br.AssertInt32(0);
                int ContainerOffset = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(ContainerOffset);
                Containers = new List<Container>(ContainerCount);
                for (int index = 0; index < ContainerCount; ++index)
                    Containers.Add(new Container(br));
                br.StepOut();
                br.StepIn(EffectOffset);
                Effects = new List<Effect>(EffectCount);
                for (int index = 0; index < EffectCount; ++index)
                    Effects.Add(new Effect(br));
                br.StepOut();
                br.StepIn(ActionOffset);
                Actions = new List<Action>(ActionCount);
                for (int index = 0; index < ActionCount; ++index)
                    Actions.Add(new Action(br));
                br.StepOut();
            }

            internal Container(Container container)
            {
                Id = container.Id;
                Containers = container.Containers.Select(container => new Container(container)).ToList();
                Effects = container.Effects.Select(effect => new Effect(effect)).ToList();
                Actions = container.Actions.Select(action => new Action(action)).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<Container> containers)
            {
                int count = containers.Count;
                bw.WriteInt16(Id);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(0);
                bw.WriteInt32(Effects.Count);
                bw.WriteInt32(Actions.Count);
                bw.WriteInt32(Containers.Count);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("ContainerEffectsOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("ContainerActionsOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("ContainerChildContainersOffset[{0}]", count));
                bw.WriteInt32(0);
                containers.Add(this);
            }

            internal void WriteContainers(BinaryWriterEx bw, List<Container> containers)
            {
                int num = containers.IndexOf(this);
                if (Containers.Count == 0)
                {
                    bw.FillInt32(string.Format("ContainerChildContainersOffset[{0}]", num), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("ContainerChildContainersOffset[{0}]", num), (int)bw.Position);
                    foreach (Container container in Containers)
                        container.Write(bw, containers);
                    foreach (Container container in Containers)
                        container.WriteContainers(bw, containers);
                }
            }

            internal void WriteEffects(BinaryWriterEx bw, int index, ref int effectCount)
            {
                if (Effects.Count == 0)
                {
                    bw.FillInt32(string.Format("ContainerEffectsOffset[{0}]", index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("ContainerEffectsOffset[{0}]", index), (int)bw.Position);
                    for (int index1 = 0; index1 < Effects.Count; ++index1)
                        Effects[index1].Write(bw, effectCount + index1);
                    effectCount += Effects.Count;
                }
            }

            internal void WriteActions(
                BinaryWriterEx bw,
                int index,
                ref int effectCount,
                List<Action> actions)
            {
                bw.FillInt32(string.Format("ContainerActionsOffset[{0}]", index), (int)bw.Position);
                foreach (Action action in Actions)
                    action.Write(bw, actions);
                for (int index1 = 0; index1 < Effects.Count; ++index1)
                    Effects[index1].WriteActions(bw, effectCount + index1, actions);
                effectCount += Effects.Count;
            }
        }

        public class Effect
        {
            [XmlAttribute] public short Id { get; set; }

            public List<Action> Actions { get; set; }

            public Effect() => Actions = new List<Action>();

            internal Effect(BinaryReaderEx br)
            {
                Id = br.ReadInt16();
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
                Actions = new List<Action>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Actions.Add(new Action(br));
                br.StepOut();
            }

            internal Effect(Effect effect)
            {
                Id = effect.Id;
                Actions = effect.Actions.Select(action => new Action(action)).ToList();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt16(Id);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(Actions.Count);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("EffectActionsOffset[{0}]", index));
                bw.WriteInt32(0);
            }

            internal void WriteActions(
                BinaryWriterEx bw,
                int index,
                List<Action> actions)
            {
                bw.FillInt32(string.Format("EffectActionsOffset[{0}]", index), (int)bw.Position);
                foreach (Action action in Actions)
                    action.Write(bw, actions);
            }
        }

        public class Action
        {
            [XmlAttribute] public short Id { get; set; }

            public bool Unk02 { get; set; }

            public bool Unk03 { get; set; }

            public int Unk04 { get; set; }

            public List<Section10> Section10s { get; set; }

            public List<Field> Fields1 { get; set; }

            public List<Field> Fields2 { get; set; }

            public List<Property> Properties1 { get; set; }

            public List<Property> Properties2 { get; set; }

            public Action()
            {
                Properties1 = new List<Property>();
                Properties2 = new List<Property>();
                Section10s = new List<Section10>();
                Fields1 = new List<Field>();
                Fields2 = new List<Field>();
            }

            internal Action(BinaryReaderEx br)
            {
                Id = br.ReadInt16(); // 0
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
                    Properties1 = new List<Property>(propertyCount1);
                    for (int index = 0; index < propertyCount1; ++index)
                        Properties1.Add(new Property(br, false));

                    Properties2 = new List<Property>(propertyCount2);
                    for (int index = 0; index < propertyCount2; ++index)
                        Properties2.Add(new Property(br, false));
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
                    Fields1 = Field.ReadMany(br, fieldCount1, this);
                    Fields2 = Field.ReadMany(br, fieldCount2, this);
                }
                br.StepOut();
            }

            internal Action(Action action)
            {
                Id = action.Id;
                Unk02 = action.Unk02;
                Unk03 = action.Unk03;
                Unk04 = action.Unk04;
                Properties1 = action.Properties1.Select(prop => new Property(prop)).ToList();
                Properties2 = action.Properties2.Select(prop => new Property(prop)).ToList();
                Section10s = action.Section10s.Select(section => new Section10(section)).ToList();
                Fields1 = action.Fields1.Select(Field.Create).ToList();
                Fields2 = action.Fields2.Select(Field.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<Action> Actions)
            {
                int count = Actions.Count;
                bw.WriteInt16(Id);
                bw.WriteBoolean(Unk02);
                bw.WriteBoolean(Unk03);
                bw.WriteInt32(Unk04);
                bw.WriteInt32(Fields1.Count);
                bw.WriteInt32(Section10s.Count);
                bw.WriteInt32(Properties1.Count);
                bw.WriteInt32(Fields2.Count);
                bw.WriteInt32(0);
                bw.WriteInt32(Properties2.Count);
                bw.ReserveInt32($"ActionFieldsOffset[{count}]");
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("ActionSection10sOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("ActionPropertiesOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                Actions.Add(this);
            }

            internal void WriteProperties(BinaryWriterEx bw, int index, List<Property> properties)
            {
                bw.FillInt32(string.Format("ActionPropertiesOffset[{0}]", index), (int)bw.Position);
                foreach (Property property in Properties1)
                    property.Write(bw, properties, false);
                foreach (Property property in Properties2)
                    property.Write(bw, properties, false);
            }

            internal void WriteSection10s(BinaryWriterEx bw, int index, List<Section10> section10s)
            {
                bw.FillInt32(string.Format("ActionSection10sOffset[{0}]", index), (int)bw.Position);
                foreach (Section10 section10 in Section10s)
                    section10.Write(bw, section10s);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                if (Fields1.Count == 0 && Fields2.Count == 0)
                {
                    bw.FillInt32(string.Format("ActionFieldsOffset[{0}]", index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("ActionFieldsOffset[{0}]", index), (int)bw.Position);
                    foreach (Field field in Fields1)
                        field.Write(bw);
                    foreach (Field field in Fields2)
                        field.Write(bw);
                    fieldCount += Fields1.Count + Fields2.Count;
                }
            }
        }

        public enum FieldType
        {
            Int,
            Float
        }

        [XmlInclude(typeof(FieldFloat))]
        [XmlInclude(typeof(FieldInt))]
        public abstract class Field
        {
            public static Field Create(float value)
            {
                return new FieldFloat(value);
            }

            public static Field Create(int value)
            {
                return new FieldInt(value);
            }

            public static Field Create(Field field)
            {
                if (field.GetType() == typeof(FieldFloat))
                {
                    return new FieldFloat(((FieldFloat)field).Value);
                }

                if (field.GetType() == typeof(FieldInt))
                {
                    return new FieldInt(((FieldInt)field).Value);
                }

                throw new InvalidOperationException("Field passed for creation was neither Float nor Int");
            }

            public static FieldFloat Create(FieldFloat field)
            {
                return new FieldFloat(field.Value);
            }

            public static FieldInt Create(FieldInt field)
            {
                return new FieldInt(field.Value);
            }

            public static Field Read(BinaryReaderEx br, object? context = null, int? index = null)
            {
                Field? field = null;
                bool isInt = false;
                // First value of interpolated properties is int (stop count), rest are floats.
                if (context is Property property)
                {
                    // Unk AC6 InterpolationType
                    if (property.InterpolationType == PropertyInterpolationType.UnkAc6)
                    {
                        if (index > 0 && index <= (int)property.PropertyType + 1)
                            isInt = true;
                    }
                    else if (property.InterpolationType != PropertyInterpolationType.Constant)
                    {
                        if (index == 0)
                            isInt = true;
                    }
                }
                else if (context is Transition transition)
                {
                    if (index == 0)
                    {
                        isInt = transition.LeftOperandType is Transition.TransitionFieldType.External;
                    }
                    else if (index == 1)
                    {
                        isInt = transition.RightOperandType is Transition.TransitionFieldType.External;
                    }
                }
                else
                {
                    // TODO: Replace heuristic with field def
                    float single = br.GetSingle(br.Position);
                    if (single >= 9.99999974737875E-05 && single < 1000000.0 ||
                        single <= -9.99999974737875E-05 && single > -1000000.0)
                        field = new FieldFloat(single);
                    else
                        isInt = true;
                }

                if (field == null)
                {
                    if (isInt)
                        field = new FieldInt(br.GetInt32(br.Position));
                    else
                        field = new FieldFloat(br.GetSingle(br.Position));
                }

                br.Position += 4L;
                return field;
            }

            public static Field ReadAt(
                BinaryReaderEx br,
                int offset,
                object? context = null,
                int? index = null)
            {
                br.StepIn(offset);
                Field field = Read(br, context, index);
                br.StepOut();
                return field;
            }

            public static List<Field> ReadMany(BinaryReaderEx br, int count, object? context = null)
            {
                List<Field> fieldList = new List<Field>();
                for (int index = 0; index < count; ++index)
                    fieldList.Add(Read(br, context, index));
                return fieldList;
            }

            public static List<Field> ReadManyAt(
                BinaryReaderEx br,
                int offset,
                int count,
                object? context = null)
            {
                br.StepIn(offset);
                List<Field> fieldList = ReadMany(br, count, context);
                br.StepOut();
                return fieldList;
            }

            public abstract void Write(BinaryWriterEx bw);

            public class FieldFloat : Field
            {
                [XmlAttribute] public float Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteSingle(Value);

                public FieldFloat(float value)
                {
                    Value = value;
                }

                public FieldFloat()
                {
                }
            }

            public class FieldInt : Field
            {
                [XmlAttribute] public int Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteInt32(Value);

                public FieldInt(int value)
                {
                    Value = value;
                }

                public FieldInt()
                {
                }
            }
        }

        public enum PropertyType
        {
            Scalar = 0,
            Vector2 = 1,
            Vector3 = 2,
            Vector4 = 3
        }

        public enum PropertyInterpolationType
        {
            Zero = 0,
            One = 1,
            Constant = 2,
            Stepped = 3,
            Linear = 4,
            Curve1 = 5,
            Curve2 = 6,
            UnkAc6 = 7
        }

        public class Property
        {
            [XmlAttribute] public PropertyType PropertyType { get; set; }

            [XmlAttribute] public PropertyInterpolationType InterpolationType { get; set; }

            [XmlAttribute] public bool IsLoop { get; set; }

            public List<Field> Fields { get; set; }

            public List<PropertyModifier> Modifiers { get; set; }

            public Property()
            {
                Modifiers = new List<PropertyModifier>();
                Fields = new List<Field>();
            }

            internal Property(BinaryReaderEx br, bool conditional)
            {
                var typeEnumA = br.ReadInt16();
                br.AssertByte(0);
                br.AssertByte(1);
                PropertyType = (PropertyType)(typeEnumA & 0b00000000_00000011);
                InterpolationType = (PropertyInterpolationType)((typeEnumA & 0b00000000_11110000) >> 4);
                IsLoop = Convert.ToBoolean((typeEnumA & 0b00010000_00000000) >> 12);
                br.ReadInt32(); // TypeEnumB
                int count = br.ReadInt32();
                br.AssertInt32(0);
                int offset = br.ReadInt32();
                br.AssertInt32(0);
                if (!conditional)
                {
                    int num3 = br.ReadInt32();
                    br.AssertInt32(0);
                    int capacity = br.ReadInt32();
                    br.AssertInt32(0);
                    br.StepIn(num3);
                    Modifiers = new List<PropertyModifier>(capacity);
                    for (int index = 0; index < capacity; ++index)
                        Modifiers.Add(new PropertyModifier(br));
                    br.StepOut();
                }

                Fields = Field.ReadManyAt(br, offset, count, this);
            }

            internal Property(Property prop)
            {
                PropertyType = prop.PropertyType;
                InterpolationType = prop.InterpolationType;
                IsLoop = prop.IsLoop;
                Modifiers = prop.Modifiers.Select(section => new PropertyModifier(section)).ToList();
                Fields = prop.Fields.Select(Field.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<Property> properties, bool conditional)
            {
                int count = properties.Count;
                int typeEnumA = (int)PropertyType | (int)InterpolationType << 4 | Convert.ToInt32(IsLoop) << 12;
                int typeEnumB = ((int)PropertyType | (int)InterpolationType << 2) + (Convert.ToInt32(IsLoop) << 4);
                bw.WriteInt16(Convert.ToInt16(typeEnumA));
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteInt32(typeEnumB);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(0);
                var offsetName = (conditional ? "Section9" : "Property") + "FieldsOffset[{0}]";
                bw.ReserveInt32(string.Format(offsetName, count));
                bw.WriteInt32(0);
                if (!conditional)
                {
                    bw.ReserveInt32(string.Format("PropertySection8sOffset[{0}]", count));
                    bw.WriteInt32(0);
                    bw.WriteInt32(Modifiers.Count);
                    bw.WriteInt32(0);
                }

                properties.Add(this);
            }

            internal void WriteModifiers(BinaryWriterEx bw, int index, List<PropertyModifier> modifiers)
            {
                bw.FillInt32(string.Format("PropertySection8sOffset[{0}]", index), (int)bw.Position);
                foreach (PropertyModifier modifier in Modifiers)
                    modifier.Write(bw, modifiers);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount, bool conditional)
            {
                var offsetName = (conditional ? "Section9" : "Property") + "FieldsOffset[{0}]";
                if (Fields.Count == 0)
                {
                    bw.FillInt32(string.Format(offsetName, index), 0);
                }
                else
                {
                    bw.FillInt32(string.Format(offsetName, index), (int)bw.Position);
                    foreach (Field field in Fields)
                        field.Write(bw);
                    fieldCount += Fields.Count;
                }
            }
        }

        public class PropertyModifier
        {
            [XmlAttribute] public ushort TypeEnumA { get; set; }

            [XmlAttribute] public uint TypeEnumB { get; set; }

            public List<Field> Fields { get; set; }

            public List<Property> Properties { get; set; }

            public PropertyModifier()
            {
                Properties = new List<Property>();
                Fields = new List<Field>();
            }

            internal PropertyModifier(BinaryReaderEx br)
            {
                TypeEnumA = br.ReadUInt16();
                br.AssertByte(0);
                br.AssertByte(1);
                TypeEnumB = br.ReadUInt32();
                int fieldCount = br.ReadInt32();
                int propertyCount = br.ReadInt32();
                int fieldOffset = br.ReadInt32();
                br.AssertInt32(0);
                int propertyOffset = br.ReadInt32();
                br.AssertInt32(0);
                br.StepIn(propertyOffset);
                Properties = new List<Property>(propertyCount);
                for (int index = 0; index < propertyCount; ++index)
                    Properties.Add(new Property(br, true));
                br.StepOut();
                Fields = Field.ReadManyAt(br, fieldOffset, fieldCount, this);
            }

            internal PropertyModifier(PropertyModifier property)
            {
                TypeEnumA = property.TypeEnumA;
                TypeEnumB = property.TypeEnumB;
                Properties = property.Properties.Select(prop => new Property(prop)).ToList();
                Fields = property.Fields.Select(Field.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<PropertyModifier> modifiers)
            {
                int count = modifiers.Count;
                bw.WriteUInt16(TypeEnumA);
                bw.WriteByte(0);
                bw.WriteByte(1);
                bw.WriteUInt32(TypeEnumB);
                bw.WriteInt32(Fields.Count);
                bw.WriteInt32(Properties.Count);
                bw.ReserveInt32(string.Format("Section8FieldsOffset[{0}]", count));
                bw.WriteInt32(0);
                bw.ReserveInt32(string.Format("Section8Section9sOffset[{0}]", count));
                bw.WriteInt32(0);
                modifiers.Add(this);
            }

            internal void WriteProperties(BinaryWriterEx bw, int index, List<Property> properties)
            {
                bw.FillInt32(string.Format("Section8Section9sOffset[{0}]", index), (int)bw.Position);
                foreach (Property property in Properties)
                    property.Write(bw, properties, true);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                bw.FillInt32(string.Format("Section8FieldsOffset[{0}]", index), (int)bw.Position);
                foreach (Field field in Fields)
                    field.Write(bw);
                fieldCount += Fields.Count;
            }
        }

        public class Section10
        {
            public List<Field> Fields { get; set; }

            public Section10() => Fields = new List<Field>();

            internal Section10(BinaryReaderEx br)
            {
                int offset = br.ReadInt32();
                br.AssertInt32(0);
                int count = br.ReadInt32();
                br.AssertInt32(0);
                Fields = Field.ReadManyAt(br, offset, count, this);
            }

            internal Section10(Section10 section)
            {
                Fields = section.Fields.Select(Field.Create).ToList();
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
                foreach (Field field in Fields)
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