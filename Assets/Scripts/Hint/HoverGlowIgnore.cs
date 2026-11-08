using UnityEngine;

/// <summary>
/// Marker: the hover outline (<see cref="HoverHighlighter"/>) skips sprites that carry this. Put on decorative child
/// sprites (the soft Glow behind the laptop, power bank and phone, and the generated Shadow) so the outline traces
/// only the real object.
/// </summary>
public class HoverGlowIgnore : MonoBehaviour { }
