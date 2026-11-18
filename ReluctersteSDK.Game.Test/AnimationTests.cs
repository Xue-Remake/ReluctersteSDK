using Microsoft.Xna.Framework;
using ReluctersteSDK.Game.Rendering;

namespace ReluctersteSDK.Game.Test;

public class AnimationClipTests
{
    [Fact]
    public void Rejects_Empty_Frame_List()
    {
        Assert.Throws<ArgumentException>(() =>
            new AnimationClip("empty", Array.Empty<AnimationFrame>()));
    }

    [Fact]
    public void Rejects_Non_Positive_Duration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnimationFrame(new Rectangle(0, 0, 8, 8), 0f));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnimationFrame(new Rectangle(0, 0, 8, 8), -1f));
    }

    [Fact]
    public void Rejects_Blank_Name()
    {
        Assert.Throws<ArgumentException>(() =>
            new AnimationClip("  ", new[] { new AnimationFrame(new Rectangle(0, 0, 8, 8), 0.1f) }));
    }

    [Fact]
    public void TotalDuration_Sums_Frame_Durations()
    {
        var clip = new AnimationClip("mix", new[]
        {
            new AnimationFrame(new Rectangle(0, 0, 8, 8), 0.1f),
            new AnimationFrame(new Rectangle(8, 0, 8, 8), 0.25f),
            new AnimationFrame(new Rectangle(16, 0, 8, 8), 0.05f),
        });

        Assert.Equal(0.4f, clip.TotalDuration, 3);
        Assert.Equal(3, clip.FrameCount);
    }

    [Fact]
    public void Frames_Is_ReadOnly_View()
    {
        AnimationClip clip = AnimationClipTestsHelper.Clip("run", 3, 0.1f);

        Assert.IsNotType<AnimationFrame[]>(clip.Frames);
        Assert.False(clip.Frames is AnimationFrame[]);
    }

    [Fact]
    public void Uniform_Constructor_Applies_Same_Duration()
    {
        var rectangles = new[]
        {
            new Rectangle(0, 0, 16, 16),
            new Rectangle(16, 0, 16, 16),
        };

        var clip = new AnimationClip("idle", rectangles, 0.2f);

        Assert.Equal(2, clip.FrameCount);
        Assert.Equal(0.2f, clip[0].Duration, 3);
        Assert.Equal(0.2f, clip[1].Duration, 3);
        Assert.Equal(0.4f, clip.TotalDuration, 3);
    }
}

public class AnimatorTests
{
    [Fact]
    public void Play_Unknown_Clip_Returns_False()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f));

        Assert.False(animator.Play("attack"));
        Assert.Null(animator.CurrentClip);
    }

    [Fact]
    public void Play_Starts_At_First_Frame()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f));

        Assert.True(animator.Play("run"));
        Assert.True(animator.IsPlaying);
        Assert.Equal(0, animator.CurrentFrameIndex);
        Assert.Equal(new Rectangle(0, 0, 16, 16), animator.CurrentSource);
    }

    [Fact]
    public void Play_Same_Clip_Without_Restart_Keeps_Progress()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f));
        animator.Play("run");
        animator.Tick(0.15f);

        Assert.Equal(1, animator.CurrentFrameIndex);

        Assert.True(animator.Play("run"));
        Assert.Equal(1, animator.CurrentFrameIndex);
    }

    [Fact]
    public void Play_With_Restart_Resets_Progress()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f));
        animator.Play("run");
        animator.Tick(0.15f);

        animator.Play("run", restart: true);

        Assert.Equal(0, animator.CurrentFrameIndex);
        Assert.Equal(0f, animator.CurrentFrameTime, 3);
    }

    [Fact]
    public void Tick_Advances_After_Frame_Duration()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f));
        animator.Play("run");

        animator.Tick(0.05f);
        Assert.Equal(0, animator.CurrentFrameIndex);

        animator.Tick(0.06f);
        Assert.Equal(1, animator.CurrentFrameIndex);
    }

    [Fact]
    public void Tick_Wraps_When_Looping()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f, loop: true));
        animator.Play("run");

        animator.Tick(0.35f);   // 跨过 3 帧，回到第 0 帧

        Assert.Equal(0, animator.CurrentFrameIndex);
        Assert.True(animator.IsPlaying);
        Assert.Null(animator.FinishedClip);
    }

    [Fact]
    public void Tick_Finishes_NonLoop_And_Holds_Last_Frame()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("attack", 3, 0.1f, loop: false));
        animator.Play("attack");

        animator.Tick(0.35f);

        Assert.False(animator.IsPlaying);
        Assert.Equal(2, animator.CurrentFrameIndex);
        Assert.Equal("attack", animator.FinishedClip);
    }

    [Fact]
    public void FinishedClip_Is_Cleared_On_Next_Tick()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("attack", 2, 0.1f, loop: false));
        animator.Play("attack");
        animator.Tick(0.25f);
        Assert.Equal("attack", animator.FinishedClip);

        animator.Tick(0.01f);

        Assert.Null(animator.FinishedClip);
    }

    [Fact]
    public void Speed_Scales_Progress()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f));
        animator.Play("run");
        animator.Speed = 2f;

        animator.Tick(0.06f);   // 等效 0.12s

        Assert.Equal(1, animator.CurrentFrameIndex);
    }

    [Fact]
    public void Pause_Stops_Progress_And_Resume_Continues()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f));
        animator.Play("run");
        animator.Tick(0.05f);

        animator.Pause();
        animator.Tick(0.2f);
        Assert.Equal(0, animator.CurrentFrameIndex);
        Assert.Equal(0.05f, animator.CurrentFrameTime, 3);

        animator.Resume();
        animator.Tick(0.06f);
        Assert.Equal(1, animator.CurrentFrameIndex);
    }

    [Fact]
    public void Stop_Resets_To_First_Frame()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f));
        animator.Play("run");
        animator.Tick(0.15f);

        animator.Stop();

        Assert.False(animator.IsPlaying);
        Assert.Equal(0, animator.CurrentFrameIndex);
        Assert.Equal(0f, animator.CurrentFrameTime, 3);
    }

    [Fact]
    public void FrameChanged_Reflects_Advancement()
    {
        var animator = new Animator(AnimationClipTestsHelper.Clip("run", 3, 0.1f));
        animator.Play("run");

        animator.Tick(0.05f);
        Assert.False(animator.FrameChanged);

        animator.Tick(0.06f);
        Assert.True(animator.FrameChanged);
    }

    [Fact]
    public void AddClip_Overrides_Same_Name()
    {
        var animator = new Animator();
        animator.AddClip(AnimationClipTestsHelper.Clip("run", 3, 0.1f));
        animator.AddClip(AnimationClipTestsHelper.Clip("run", 5, 0.1f));

        Assert.Equal(1, animator.ClipCount);
        animator.Play("run");
        Assert.Equal(5, animator.CurrentClip!.FrameCount);
    }
}

public class SpriteSheetTests
{
    [Fact]
    public void Slice_Computes_Row_Major_Grid()
    {
        Rectangle[] frames = SpriteSheet.Slice(48, 32, 16, 16);

        Assert.Equal(6, frames.Length);
        Assert.Equal(new Rectangle(0, 0, 16, 16), frames[0]);
        Assert.Equal(new Rectangle(16, 0, 16, 16), frames[1]);
        Assert.Equal(new Rectangle(32, 0, 16, 16), frames[2]);
        Assert.Equal(new Rectangle(0, 16, 16, 16), frames[3]);
    }

    [Fact]
    public void Slice_Honors_StartIndex_And_Count()
    {
        Rectangle[] frames = SpriteSheet.Slice(48, 32, 16, 16, startIndex: 2, frameCount: 2);

        Assert.Equal(2, frames.Length);
        Assert.Equal(new Rectangle(32, 0, 16, 16), frames[0]);
        Assert.Equal(new Rectangle(0, 16, 16, 16), frames[1]);
    }

    [Fact]
    public void Slice_Uses_Explicit_Columns()
    {
        Rectangle[] frames = SpriteSheet.Slice(48, 32, 16, 16, columns: 2);

        Assert.Equal(4, frames.Length);
        Assert.Equal(new Rectangle(16, 0, 16, 16), frames[1]);
        Assert.Equal(new Rectangle(0, 16, 16, 16), frames[2]);
    }

    [Fact]
    public void Slice_Rejects_Frame_Outside_Texture()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SpriteSheet.Slice(48, 32, 16, 16, columns: 4));   // 第 4 列越界
    }

    [Fact]
    public void Slice_Rejects_Invalid_Sizes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SpriteSheet.Slice(48, 32, 0, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpriteSheet.Slice(0, 32, 16, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpriteSheet.Slice(48, 32, 16, 16, startIndex: -1));
    }

    [Fact]
    public void Slice_Rejects_Count_Beyond_Textures()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SpriteSheet.Slice(48, 32, 16, 16, startIndex: 5, frameCount: 3));
    }
}

internal static class AnimationClipTestsHelper
{
    public static AnimationClip Clip(string name, int frameCount, float duration, bool loop = true)
    {
        var rectangles = new Rectangle[frameCount];
        for (int i = 0; i < frameCount; i++)
            rectangles[i] = new Rectangle(i * 16, 0, 16, 16);

        return new AnimationClip(name, rectangles, duration, loop);
    }
}
