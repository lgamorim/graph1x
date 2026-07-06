namespace Graph1x.Serialization;

/// <summary>
/// Flow direction of a Mermaid flowchart, emitted in the
/// <c>flowchart</c> header.
/// </summary>
public enum MermaidDirection
{
    /// <summary>Top to bottom (<c>TD</c>). The Mermaid default.</summary>
    TopDown,

    /// <summary>Left to right (<c>LR</c>).</summary>
    LeftToRight,

    /// <summary>Bottom to top (<c>BT</c>).</summary>
    BottomToTop,

    /// <summary>Right to left (<c>RL</c>).</summary>
    RightToLeft,
}
