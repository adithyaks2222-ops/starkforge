// ============================================================
// LogicGate.cs — Core Logic Engine (Pure C#)
// Standard .NET / Unity-ready (Zero external dependencies)
// Gates: INPUT, OUTPUT, AND, OR, NOT, NAND, NOR, XOR
// ============================================================
using System;
using System.Collections.Generic;

namespace StarkForge.Core
{
    public enum GateType { INPUT, OUTPUT, AND, OR, NOT, NAND, NOR, XOR }

    // Lightweight 2D vector for gate positioning without requiring UnityEngine
    public struct Vector2
    {
        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X}, {Y})";
    }

    [Serializable]
    public class LogicGate
    {
        public string Id { get; set; }
        public GateType Type { get; set; }
        public string Label { get; set; }

        public bool State { get; set; }
        public string Expression { get; set; }

        public Vector2 Position { get; set; }

        public int InputCount => (Type == GateType.INPUT)  ? 0
                               : (Type == GateType.NOT)    ? 1
                               : (Type == GateType.OUTPUT) ? 1
                               : 2;   // AND / OR / NAND / NOR / XOR

        public int OutputCount => (Type == GateType.OUTPUT) ? 0 : 1;

        public LogicGate(GateType type, string label, Vector2 position)
        {
            Id         = Guid.NewGuid().ToString();
            Type       = type;
            Label      = label;
            Position   = position;
            State      = false;
            Expression = (type == GateType.INPUT) ? label : null;
        }

        public void Evaluate(bool[] inputStates, string[] inputExprs)
        {
            if (Type == GateType.INPUT)
            {
                Expression = Label;
                return;
            }

            if (inputStates == null || inputExprs == null) return;

            switch (Type)
            {
                case GateType.NOT:
                    if (inputStates.Length < 1 || inputExprs[0] == null) { State = false; Expression = null; return; }
                    State = !inputStates[0];
                    Expression = $"{WrapExpr(inputExprs[0], GateType.NOT)}'";
                    break;

                case GateType.OUTPUT:
                    if (inputStates.Length < 1 || inputExprs[0] == null) { State = false; Expression = null; return; }
                    State = inputStates[0];
                    Expression = inputExprs[0];
                    break;

                case GateType.AND:
                    if (inputStates.Length < 2 || inputExprs[0] == null || inputExprs[1] == null) { State = false; Expression = null; return; }
                    State = inputStates[0] && inputStates[1];
                    Expression = $"{WrapExpr(inputExprs[0], GateType.AND)}{WrapExpr(inputExprs[1], GateType.AND)}";
                    break;

                case GateType.OR:
                    if (inputStates.Length < 2 || inputExprs[0] == null || inputExprs[1] == null) { State = false; Expression = null; return; }
                    State = inputStates[0] || inputStates[1];
                    Expression = $"{inputExprs[0]}+{inputExprs[1]}";
                    break;

                case GateType.NAND:
                    if (inputStates.Length < 2 || inputExprs[0] == null || inputExprs[1] == null) { State = false; Expression = null; return; }
                    State = !(inputStates[0] && inputStates[1]);
                    Expression = $"({WrapExpr(inputExprs[0], GateType.AND)}{WrapExpr(inputExprs[1], GateType.AND)})'";
                    break;

                case GateType.NOR:
                    if (inputStates.Length < 2 || inputExprs[0] == null || inputExprs[1] == null) { State = false; Expression = null; return; }
                    State = !(inputStates[0] || inputStates[1]);
                    Expression = $"({inputExprs[0]}+{inputExprs[1]})'";
                    break;

                case GateType.XOR:
                    if (inputStates.Length < 2 || inputExprs[0] == null || inputExprs[1] == null) { State = false; Expression = null; return; }
                    State = inputStates[0] ^ inputStates[1];
                    Expression = $"{inputExprs[0]}\u2295{inputExprs[1]}";
                    break;
            }
        }

        private string WrapExpr(string expr, GateType parentType)
        {
            if (expr == null) return "?";
            if (expr.Length == 1) return expr;
            if (expr.EndsWith("'")) return expr;
            if (parentType == GateType.AND && expr.Contains("+")) return $"({expr})";
            return expr;
        }
    }

    // ── Wire: connects an output port of one gate to an input port of another ──
    [Serializable]
    public class Wire
    {
        public string Id { get; set; }
        public string FromGateId { get; set; }
        public int    FromPortIndex { get; set; }
        public string ToGateId { get; set; }
        public int    ToPortIndex { get; set; }
        public bool   State { get; set; }

        public Wire(string fromGateId, int fromPort, string toGateId, int toPort)
        {
            Id            = Guid.NewGuid().ToString();
            FromGateId    = fromGateId;
            FromPortIndex = fromPort;
            ToGateId      = toGateId;
            ToPortIndex   = toPort;
            State         = false;
        }
    }

    // ── Circuit: holds all gates + wires and runs the propagation engine ──────
    public class Circuit
    {
        public List<LogicGate> Gates { get; } = new List<LogicGate>();
        public List<Wire>      Wires { get; } = new List<Wire>();

        private Dictionary<string, LogicGate> _gateById = new Dictionary<string, LogicGate>();

        public void AddGate(LogicGate gate)
        {
            Gates.Add(gate);
            RebuildIndex();
        }

        public void RemoveGate(string gateId)
        {
            Gates.RemoveAll(g => g.Id == gateId);
            Wires.RemoveAll(w => w.FromGateId == gateId || w.ToGateId == gateId);
            RebuildIndex();
        }

        public void AddWire(Wire wire)
        {
            Wires.RemoveAll(w => w.ToGateId == wire.ToGateId && w.ToPortIndex == wire.ToPortIndex);
            Wires.Add(wire);
        }

        public void RemoveWireAtInputPort(string toGateId, int toPort)
        {
            Wires.RemoveAll(w => w.ToGateId == toGateId && w.ToPortIndex == toPort);
        }

        public void Clear()
        {
            Gates.Clear();
            Wires.Clear();
            _gateById.Clear();
        }

        private void RebuildIndex()
        {
            _gateById.Clear();
            foreach (var g in Gates) _gateById[g.Id] = g;
        }

        public LogicGate GetGate(string id)
        {
            _gateById.TryGetValue(id, out var g);
            return g;
        }

        public void Evaluate()
        {
            RebuildIndex();

            foreach (var g in Gates)
            {
                if (g.Type != GateType.INPUT)
                {
                    g.State = false;
                    g.Expression = null;
                }
            }

            bool changed = true;
            int  guard   = Gates.Count + 4;

            while (changed && guard-- > 0)
            {
                changed = false;
                foreach (var gate in Gates)
                {
                    if (gate.Type == GateType.INPUT) continue;

                    bool[]   inputStates = new bool[gate.InputCount];
                    string[] inputExprs  = new string[gate.InputCount];
                    bool ready = true;

                    for (int i = 0; i < gate.InputCount; i++)
                    {
                        var wire = Wires.Find(w => w.ToGateId == gate.Id && w.ToPortIndex == i);
                        if (wire == null) { ready = false; break; }
                        var src = GetGate(wire.FromGateId);
                        if (src == null || src.Expression == null) { ready = false; break; }
                        inputStates[i] = src.State;
                        inputExprs[i]  = src.Expression;
                    }

                    if (!ready) continue;

                    bool   prevState = gate.State;
                    string prevExpr  = gate.Expression;
                    gate.Evaluate(inputStates, inputExprs);

                    if (gate.State != prevState || gate.Expression != prevExpr)
                        changed = true;
                }
            }

            foreach (var wire in Wires)
            {
                var src = GetGate(wire.FromGateId);
                wire.State = src != null && src.State;
            }
        }

        public TruthTable GenerateTruthTable()
        {
            var inputs  = Gates.FindAll(g => g.Type == GateType.INPUT);
            var outputs = Gates.FindAll(g => g.Type == GateType.OUTPUT);

            if (inputs.Count == 0 || outputs.Count == 0)
                return null;

            int rows = 1 << inputs.Count;
            var table = new TruthTable(inputs, outputs, rows);

            bool[] savedStates = new bool[inputs.Count];
            for (int i = 0; i < inputs.Count; i++) savedStates[i] = inputs[i].State;

            for (int r = 0; r < rows; r++)
            {
                for (int i = 0; i < inputs.Count; i++)
                {
                    inputs[i].State = (r & (1 << (inputs.Count - 1 - i))) != 0;
                    inputs[i].Expression = inputs[i].Label;
                }
                Evaluate();

                table.InputRows[r]  = new bool[inputs.Count];
                table.OutputRows[r] = new bool[outputs.Count];
                for (int i = 0; i < inputs.Count;  i++) table.InputRows[r][i]  = inputs[i].State;
                for (int o = 0; o < outputs.Count; o++) table.OutputRows[r][o] = outputs[o].State;
            }

            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].State = savedStates[i];
                inputs[i].Expression = inputs[i].Label;
            }
            Evaluate();

            return table;
        }
    }

    public class TruthTable
    {
        public List<LogicGate> Inputs { get; }
        public List<LogicGate> Outputs { get; }
        public bool[][]        InputRows { get; }
        public bool[][]        OutputRows { get; }

        public TruthTable(List<LogicGate> inputs, List<LogicGate> outputs, int rowCount)
        {
            Inputs     = inputs;
            Outputs    = outputs;
            InputRows  = new bool[rowCount][];
            OutputRows = new bool[rowCount][];
        }
    }
}
