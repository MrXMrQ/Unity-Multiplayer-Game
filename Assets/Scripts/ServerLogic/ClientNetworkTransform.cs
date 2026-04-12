using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    // Damit sagen wir Unity: Der Client hat recht, nicht der Server!
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}