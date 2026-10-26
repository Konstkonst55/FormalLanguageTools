using FormalLanguageTools.FiniteAutomata.Models;
using Microsoft.Msagl.Drawing;

namespace FormalLanguageTools.App.Services;

public sealed class AutomatonGraphBuilder
{
    public Graph CreateGraph(IFiniteAutomaton automaton)
    {
        ArgumentNullException.ThrowIfNull(automaton);
        var graph = new Graph();
        graph.Attr.BackgroundColor = Color.White;

        foreach (var state in automaton.States)
        {
            var isInitial = state == automaton.InitialState;
            var isFinal = automaton.FinalStates.Contains(state, StringComparer.Ordinal);
            var label = $"{(isInitial ? "→ " : string.Empty)}{(isFinal ? "*" : string.Empty)}{state}";
            var node = graph.AddNode(state);
            node.LabelText = label;
            node.Attr.Shape = isFinal ? Shape.DoubleCircle : Shape.Ellipse;
            node.Attr.Color = Color.DarkSlateBlue;
            node.Attr.FillColor = Color.White;
            node.Attr.LineWidth = 1.5;
        }

        var alphabetOrder = automaton.Alphabet.Select((symbol, index) => (symbol, index))
            .ToDictionary(item => item.symbol, item => item.index, StringComparer.Ordinal);
        foreach (var group in automaton.Transitions.GroupBy(transition => (transition.FromState, transition.ToState)))
        {
            var symbols = group.Select(transition => transition.Symbol)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(symbol => alphabetOrder[symbol])
                .ToArray();
            var edge = graph.AddEdge(group.Key.FromState, string.Join(", ", symbols), group.Key.ToState);
            edge.Attr.Color = Color.DimGray;
            edge.Attr.LineWidth = 1.2;
        }

        return graph;
    }
}
