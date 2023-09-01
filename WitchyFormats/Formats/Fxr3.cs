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
    public class Fxr3 : SoulsFile<Fxr3>
    {
        public FXRVersion Version { get; set; }

        public int Id { get; set; }

        public FFXStateMachine RootStateMachine { get; set; }

        public FFXContainer RootContainer { get; set; }

        public List<int> References { get; set; }

        public List<int> UnkExternalValues { get; set; }

        public List<int> UnkBloodEnabler { get; set; }

        public List<int> UnkEmpty { get; set; }

        public Fxr3()
        {
            Version = FXRVersion.DarkSouls3;
            RootStateMachine = new FFXStateMachine();
            RootContainer = new FFXContainer();
            References = new List<int>();
            UnkExternalValues = new List<int>();
            UnkBloodEnabler = new List<int>();
            UnkEmpty = new List<int>();
        }

        public Fxr3(Fxr3 fxr)
        {
            Id = fxr.Id;
            Version = fxr.Version;
            RootStateMachine = new FFXStateMachine(fxr.RootStateMachine);
            RootContainer = new FFXContainer(fxr.RootContainer);
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
            int stateMachineOffset = br.ReadInt32();
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

            br.Position = stateMachineOffset;
            RootStateMachine = new FFXStateMachine(br);

            br.Position = containerOffset;
            RootContainer = new FFXContainer(br);
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
            bw.ReserveInt32("ContainerOffset");
            bw.ReserveInt32("ContainerCount");
            bw.ReserveInt32("EffectOffset");
            bw.ReserveInt32("EffectCount");
            bw.ReserveInt32("ActionOffset");
            bw.ReserveInt32("ActionCount");
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

            bw.FillInt32("StateMachineOffset", (int)bw.Position);
            RootStateMachine.Write(bw);
            bw.Pad(16);
            bw.FillInt32("FFXStateOffset", (int)bw.Position);
            RootStateMachine.WriteStates(bw);
            bw.Pad(16);
            bw.FillInt32("FFXTransitionOffset", (int)bw.Position);
            List<FFXState> states = RootStateMachine.States;
            List<FFXTransition> transitions = new List<FFXTransition>();
            for (int index = 0; index < states.Count; ++index)
                states[index].WriteTransitions(bw, index, transitions);
            bw.FillInt32("FFXTransitionCount", transitions.Count);
            bw.Pad(16);
            bw.FillInt32("ContainerOffset", (int)bw.Position);
            List<FFXContainer> Containers = new List<FFXContainer>();
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
            List<FFXAction> actions = new List<FFXAction>();
            for (int index = 0; index < Containers.Count; ++index)
                Containers[index].WriteActions(bw, index, ref EffectCount, actions);
            bw.FillInt32("ActionCount", actions.Count);
            bw.Pad(16);
            bw.FillInt32("FFXPropertyOffset", (int)bw.Position);
            List<FFXProperty> properties = new List<FFXProperty>();
            for (int index = 0; index < actions.Count; ++index)
                actions[index].WriteProperties(bw, index, properties);
            bw.FillInt32("FFXPropertyCount", properties.Count);
            bw.Pad(16);
            bw.FillInt32("Section8Offset", (int)bw.Position);
            List<PropertyModifier> modifiers = new List<PropertyModifier>();
            for (int index = 0; index < properties.Count; ++index)
                properties[index].WriteModifiers(bw, index, modifiers);
            bw.FillInt32("Section8Count", modifiers.Count);
            bw.Pad(16);
            bw.FillInt32("Section9Offset", (int)bw.Position);
            List<FFXProperty> conditionalProperties = new List<FFXProperty>();
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

            public FFXField Field1 { get; set; }

            public FFXField Field2 { get; set; }

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
                Field1 = FFXField.ReadAt(br, fieldOffset1, this, 0);
                Field2 = FFXField.ReadAt(br, fieldOffset2, this, 1);
            }

            internal FFXTransition(FFXTransition transition)
            {
                Unk00 = transition.Unk00;
                TargetStateIndex = transition.TargetStateIndex;
                Unk10 = transition.Unk10;
                Unk38 = transition.Unk38;
                Unk40 = transition.Unk40;
                Field1 = transition.Field1;
                Field2 = transition.Field2;
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
                Field1.Write(bw);
                bw.FillInt32(string.Format("TransitionFieldOffset2[{0}]", index), (int)bw.Position);
                Field2.Write(bw);
                fieldCount += 2;
            }
        }

        public class FFXContainer
        {
            [XmlAttribute] public short Id { get; set; }

            public List<FFXAction> Actions { get; set; }
            public List<FFXEffect> Effects { get; set; }
            public List<FFXContainer> Containers { get; set; }

            public FFXContainer()
            {
                Containers = new List<FFXContainer>();
                Effects = new List<FFXEffect>();
                Actions = new List<FFXAction>();
            }

            internal FFXContainer(BinaryReaderEx br)
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
                Containers = new List<FFXContainer>(ContainerCount);
                for (int index = 0; index < ContainerCount; ++index)
                    Containers.Add(new FFXContainer(br));
                br.StepOut();
                br.StepIn(EffectOffset);
                Effects = new List<FFXEffect>(EffectCount);
                for (int index = 0; index < EffectCount; ++index)
                    Effects.Add(new FFXEffect(br));
                br.StepOut();
                br.StepIn(ActionOffset);
                Actions = new List<FFXAction>(ActionCount);
                for (int index = 0; index < ActionCount; ++index)
                    Actions.Add(new FFXAction(br));
                br.StepOut();
            }

            internal FFXContainer(FFXContainer container)
            {
                Id = container.Id;
                Containers = container.Containers.Select(container => new FFXContainer(container)).ToList();
                Effects = container.Effects.Select(effect => new FFXEffect(effect)).ToList();
                Actions = container.Actions.Select(action => new FFXAction(action)).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<FFXContainer> containers)
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

            internal void WriteContainers(BinaryWriterEx bw, List<FFXContainer> containers)
            {
                int num = containers.IndexOf(this);
                if (Containers.Count == 0)
                {
                    bw.FillInt32(string.Format("ContainerChildContainersOffset[{0}]", num), 0);
                }
                else
                {
                    bw.FillInt32(string.Format("ContainerChildContainersOffset[{0}]", num), (int)bw.Position);
                    foreach (FFXContainer container in Containers)
                        container.Write(bw, containers);
                    foreach (FFXContainer container in Containers)
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
                List<FFXAction> actions)
            {
                bw.FillInt32(string.Format("ContainerActionsOffset[{0}]", index), (int)bw.Position);
                foreach (FFXAction action in Actions)
                    action.Write(bw, actions);
                for (int index1 = 0; index1 < Effects.Count; ++index1)
                    Effects[index1].WriteActions(bw, effectCount + index1, actions);
                effectCount += Effects.Count;
            }
        }

        public class FFXEffect
        {
            [XmlAttribute] public short Id { get; set; }

            public List<FFXAction> Actions { get; set; }

            public FFXEffect() => Actions = new List<FFXAction>();

            internal FFXEffect(BinaryReaderEx br)
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
                Actions = new List<FFXAction>(capacity);
                for (int index = 0; index < capacity; ++index)
                    Actions.Add(new FFXAction(br));
                br.StepOut();
            }

            internal FFXEffect(FFXEffect effect)
            {
                Id = effect.Id;
                Actions = effect.Actions.Select(action => new FFXAction(action)).ToList();
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
                List<FFXAction> actions)
            {
                bw.FillInt32(string.Format("EffectActionsOffset[{0}]", index), (int)bw.Position);
                foreach (FFXAction action in Actions)
                    action.Write(bw, actions);
            }
        }

        public class FFXAction
        {
            [XmlAttribute] public short Id { get; set; }

            public bool Unk02 { get; set; }

            public bool Unk03 { get; set; }

            public int Unk04 { get; set; }

            public List<Section10> Section10s { get; set; }

            public List<FFXField> Fields1 { get; set; }

            public List<FFXField> Fields2 { get; set; }

            public List<FFXProperty> Properties1 { get; set; }

            public List<FFXProperty> Properties2 { get; set; }

            public FFXAction()
            {
                Properties1 = new List<FFXProperty>();
                Properties2 = new List<FFXProperty>();
                Section10s = new List<Section10>();
                Fields1 = new List<FFXField>();
                Fields2 = new List<FFXField>();
            }

            internal FFXAction(BinaryReaderEx br)
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
                    Properties1 = new List<FFXProperty>(propertyCount1);
                    for (int index = 0; index < propertyCount1; ++index)
                        Properties1.Add(new FFXProperty(br, false));

                    Properties2 = new List<FFXProperty>(propertyCount2);
                    for (int index = 0; index < propertyCount2; ++index)
                        Properties2.Add(new FFXProperty(br, false));
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
                    Fields1 = FFXField.ReadMany(br, fieldCount1, this);
                    Fields2 = FFXField.ReadMany(br, fieldCount2, this);
                }
                br.StepOut();
            }

            internal FFXAction(FFXAction action)
            {
                Id = action.Id;
                Unk02 = action.Unk02;
                Unk03 = action.Unk03;
                Unk04 = action.Unk04;
                Properties1 = action.Properties1.Select(prop => new FFXProperty(prop)).ToList();
                Properties2 = action.Properties2.Select(prop => new FFXProperty(prop)).ToList();
                Section10s = action.Section10s.Select(section => new Section10(section)).ToList();
                Fields1 = action.Fields1.Select(FFXField.Create).ToList();
                Fields2 = action.Fields2.Select(FFXField.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<FFXAction> Actions)
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

            internal void WriteProperties(BinaryWriterEx bw, int index, List<FFXProperty> ffxProperties)
            {
                bw.FillInt32(string.Format("ActionPropertiesOffset[{0}]", index), (int)bw.Position);
                foreach (FFXProperty property in Properties1)
                    property.Write(bw, ffxProperties, false);
                foreach (FFXProperty property in Properties2)
                    property.Write(bw, ffxProperties, false);
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
                    foreach (FFXField ffxField in Fields1)
                        ffxField.Write(bw);
                    foreach (FFXField ffxField in Fields2)
                        ffxField.Write(bw);
                    fieldCount += Fields1.Count + Fields2.Count;
                }
            }
        }

        public enum FieldType
        {
            Int,
            Float
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
                    return new FFXFieldFloat(((FFXFieldFloat)field).Value);
                }

                if (field.GetType() == typeof(FFXFieldInt))
                {
                    return new FFXFieldInt(((FFXFieldInt)field).Value);
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

            public static FFXField Read(BinaryReaderEx br, object? context = null, int? index = null)
            {
                FFXField? ffxField = null;
                bool isInt = false;
                // First value of interpolated properties is int (stop count), rest are floats.
                if (context is FFXProperty property)
                {
                    // Unk AC6 InterpolationType
                    if (property.InterpolationType == PropertyInterpolationType.UnkAc6)
                    {
                        if (index > 0 && index <= (int)property.PropertyType + 1)
                            isInt = true;
                    }
                    else if (property.InterpolationType != PropertyInterpolationType.StaticValue)
                    {
                        if (index == 0)
                            isInt = true;
                    }
                }
                // Needs confirmation: First transition's first value seems to always be a float.
                else if (context is FFXTransition transition)
                {
                    if (transition.TargetStateIndex == -1)
                        isInt = index != 0;
                    else
                        isInt = index != 1;
                }
                else
                {
                    // TODO: Replace heuristic with field def
                    float single = br.GetSingle(br.Position);
                    if (single >= 9.99999974737875E-05 && single < 1000000.0 ||
                        single <= -9.99999974737875E-05 && single > -1000000.0)
                        ffxField = new FFXFieldFloat(single);
                    else
                        isInt = true;
                }

                if (ffxField == null)
                {
                    if (isInt)
                        ffxField = new FFXFieldInt(br.GetInt32(br.Position));
                    else
                        ffxField = new FFXFieldFloat(br.GetSingle(br.Position));
                }

                br.Position += 4L;
                return ffxField;
            }

            public static FFXField ReadAt(
                BinaryReaderEx br,
                int offset,
                object? context = null,
                int? index = null)
            {
                br.StepIn(offset);
                FFXField field = Read(br, context, index);
                br.StepOut();
                return field;
            }

            public static List<FFXField> ReadMany(BinaryReaderEx br, int count, object? context = null)
            {
                List<FFXField> ffxFieldList = new List<FFXField>();
                for (int index = 0; index < count; ++index)
                    ffxFieldList.Add(Read(br, context, index));
                return ffxFieldList;
            }

            public static List<FFXField> ReadManyAt(
                BinaryReaderEx br,
                int offset,
                int count,
                object? context = null)
            {
                br.StepIn(offset);
                List<FFXField> ffxFieldList = ReadMany(br, count, context);
                br.StepOut();
                return ffxFieldList;
            }

            public abstract void Write(BinaryWriterEx bw);

            public class FFXFieldFloat : FFXField
            {
                [XmlAttribute] public float Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteSingle(Value);

                public FFXFieldFloat(float value)
                {
                    Value = value;
                }

                public FFXFieldFloat()
                {
                }
            }

            public class FFXFieldInt : FFXField
            {
                [XmlAttribute] public int Value;

                public override void Write(BinaryWriterEx bw) => bw.WriteInt32(Value);

                public FFXFieldInt(int value)
                {
                    Value = value;
                }

                public FFXFieldInt()
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
            StaticValue = 2,
            Stepped = 3,
            Linear = 4,
            Curve1 = 5,
            Curve2 = 6,
            UnkAc6 = 7
        }

        public class FFXProperty
        {
            [XmlAttribute] public PropertyType PropertyType { get; set; }

            [XmlAttribute] public PropertyInterpolationType InterpolationType { get; set; }

            [XmlAttribute] public bool IsLoop { get; set; }

            public List<FFXField> Fields { get; set; }

            public List<PropertyModifier> Modifiers { get; set; }

            public FFXProperty()
            {
                Modifiers = new List<PropertyModifier>();
                Fields = new List<FFXField>();
            }

            internal FFXProperty(BinaryReaderEx br, bool conditional)
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

                Fields = FFXField.ReadManyAt(br, offset, count, this);
            }

            internal FFXProperty(FFXProperty prop)
            {
                PropertyType = prop.PropertyType;
                InterpolationType = prop.InterpolationType;
                IsLoop = prop.IsLoop;
                Modifiers = prop.Modifiers.Select(section => new PropertyModifier(section)).ToList();
                Fields = prop.Fields.Select(FFXField.Create).ToList();
            }

            internal void Write(BinaryWriterEx bw, List<FFXProperty> properties, bool conditional)
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
                    foreach (FFXField field in Fields)
                        field.Write(bw);
                    fieldCount += Fields.Count;
                }
            }
        }

        public class PropertyModifier
        {
            [XmlAttribute] public ushort TypeEnumA { get; set; }

            [XmlAttribute] public uint TypeEnumB { get; set; }

            public List<FFXField> Fields { get; set; }

            public List<FFXProperty> Properties { get; set; }

            public PropertyModifier()
            {
                Properties = new List<FFXProperty>();
                Fields = new List<FFXField>();
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
                Properties = new List<FFXProperty>(propertyCount);
                for (int index = 0; index < propertyCount; ++index)
                    Properties.Add(new FFXProperty(br, true));
                br.StepOut();
                Fields = FFXField.ReadManyAt(br, fieldOffset, fieldCount, this);
            }

            internal PropertyModifier(PropertyModifier property)
            {
                TypeEnumA = property.TypeEnumA;
                TypeEnumB = property.TypeEnumB;
                Properties = property.Properties.Select(prop => new FFXProperty(prop)).ToList();
                Fields = property.Fields.Select(FFXField.Create).ToList();
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

            internal void WriteProperties(BinaryWriterEx bw, int index, List<FFXProperty> properties)
            {
                bw.FillInt32(string.Format("Section8Section9sOffset[{0}]", index), (int)bw.Position);
                foreach (FFXProperty property in Properties)
                    property.Write(bw, properties, true);
            }

            internal void WriteFields(BinaryWriterEx bw, int index, ref int fieldCount)
            {
                bw.FillInt32(string.Format("Section8FieldsOffset[{0}]", index), (int)bw.Position);
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
                Fields = FFXField.ReadManyAt(br, offset, count, this);
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