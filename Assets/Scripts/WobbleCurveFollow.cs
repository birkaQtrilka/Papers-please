using System;
using Unity.Mathematics;
using UnityEngine;

public sealed class WobbleCurveFollow : IDisposable
{
    public readonly CurveFollower follower;
    private readonly Transform transform;
    readonly float wobbleSpeed = 10f;
    readonly float wobbleAmplitude = 3f;
    readonly float startY;
    float time;

    public WobbleCurveFollow(CurveFollower follower, float wobbleSpeed, float wobbleAmplitude)
    {
        this.follower = follower;
        transform = follower.walker.GetChild(0);
        startY = transform.position.y;
        this.wobbleAmplitude = wobbleAmplitude;
        this.wobbleSpeed = wobbleSpeed;
        follower.OnWalk += Follower_OnWalk;
    }

    private void Follower_OnWalk()
    {
        time += Time.deltaTime * wobbleSpeed;
        transform.position = new Vector3(
            transform.position.x, 
            startY + Remap01(math.sin(time)) * wobbleAmplitude,
            transform.position.z
        );

    }

    public void Dispose()
    {
        follower.OnWalk -= Follower_OnWalk;
    }

    float Remap01(float n)
    {
        return (n + 1f) * 0.5f;
    }
}
