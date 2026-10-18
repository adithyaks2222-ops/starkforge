// ============================================================
// CircuitManager.cs — Core Circuit Controller (Pure C#)
// Standard C# event-driven controller for logic circuit state
// ============================================================
using System;
using System.Collections.Generic;

namespace StarkForge.Core
{
    public class CircuitManager
    {
        private static CircuitManager _instance;
        public static CircuitManager Instance => _instance ??= new CircuitManager();

        public event Action OnCircuitChanged;

        public Circuit Circuit { get; private set; } = new Circuit();

        public CircuitManager()
        {
            _instance = this;
        }

        public LogicGate AddGate(GateType type, string label, Vector2 position)
        {
            var gate = new LogicGate(type, label, position);
            Circuit.AddGate(gate);
            Evaluate();
            return gate;
        }

        public void RemoveGate(string id)
        {
            Circuit.RemoveGate(id);
            Evaluate();
        }

        public bool TryAddWire(string fromId, int fromPort, string toId, int toPort)
        {
            if (fromId == toId) return false;
            if (WouldCreateCycle(fromId, toId)) return false;

            Circuit.AddWire(new Wire(fromId, fromPort, toId, toPort));
            Evaluate();
            return true;
        }

        public void DisconnectInputPort(string gateId, int portIndex)
        {
            Circuit.RemoveWireAtInputPort(gateId, portIndex);
            Evaluate();
        }

        public void ToggleInput(string gateId)
        {
            var gate = Circuit.GetGate(gateId);
            if (gate != null && gate.Type == GateType.INPUT)
            {
                gate.State = !gate.State;
                gate.Expression = gate.Label;
                Evaluate();
            }
        }

        public void SetInputState(string gateId, bool state)
        {
            var gate = Circuit.GetGate(gateId);
            if (gate != null && gate.Type == GateType.INPUT)
            {
                gate.State = state;
                gate.Expression = gate.Label;
                Evaluate();
            }
        }

        public void ClearAll()
        {
            Circuit.Clear();
            OnCircuitChanged?.Invoke();
        }

        public void Evaluate()
        {
            Circuit.Evaluate();
            OnCircuitChanged?.Invoke();
        }

        public LogicGate GetOutputGate()
        {
            return Circuit.Gates.Find(g => g.Type == GateType.OUTPUT);
        }

        public TruthTable GetTruthTable() => Circuit.GenerateTruthTable();

        private bool WouldCreateCycle(string fromId, string toId)
        {
            var visited = new HashSet<string>();
            var stack   = new Stack<string>();
            stack.Push(toId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == fromId) return true;
                if (visited.Contains(current)) continue;
                visited.Add(current);

                foreach (var wire in Circuit.Wires)
                {
                    if (wire.FromGateId == current)
                        stack.Push(wire.ToGateId);
                }
            }
            return false;
        }
    }
}
