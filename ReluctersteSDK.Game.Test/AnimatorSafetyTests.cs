using Microsoft.Xna.Framework;
using ReluctersteSDK.Game.Rendering;

namespace ReluctersteSDK.Game.Test;

public class AnimatorSafetyTests
{
    private static Animator CreateAnimator()
    {
        var clip = new AnimationClip(
            "idle",
            new[] { new Rectangle(0, 0, 16, 16) },
            frameDuration: 0.1f,
            loop: true);
        return new Animator(clip);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void Invalid_Speed_Is_Ignored(float speed)
    {
        var animator = CreateAnimator();
        animator.Speed = speed;
        Assert.Equal(1f, animator.Speed);
    }

    [Fact]
    public void Huge_DeltaTime_Does_Not_Hang()
    {
        var animator = CreateAnimator();
        animator.Play("idle");

        animator.Tick(1e9f);

        Assert.True(animator.IsPlaying);
    }
}
