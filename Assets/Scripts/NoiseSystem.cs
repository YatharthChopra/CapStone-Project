using System;
using UnityEngine;

// A simple "noise" announcement that enemies listen to.
// Anything that makes a sound calls:  NoiseSystem.MakeNoise(position);
// Every enemy close enough to hear it reacts (see EnemyBase.HearNoise).
// We have no real sounds yet, so for now only the test key (N) calls this.
// Later the audio code can call it (a thrown object landing, a door slamming, etc).
public static class NoiseSystem
{
    // Enemies subscribe to this. It tells them where the noise was and how loud it was.
    public static event Action<Vector2, float> NoiseMade;

    // loudness 1 = normal. A louder noise (2) can be heard from twice as far away.
    public static void MakeNoise(Vector2 position, float loudness = 1f)
    {
        if (NoiseMade != null)
        {
            NoiseMade(position, loudness);
        }
    }
}
