using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReluctersteSDK.Game.Rendering
{
    /// <summary>
    /// 序列图（sprite sheet）切分工具：把一张按网格排布的纹理切成一组源矩形。
    /// </summary>
    public static class SpriteSheet
    {
        /// <summary>
        /// 按网格切分纹理，返回每个格子的源矩形。
        /// </summary>
        /// <param name="textureWidth">纹理宽度。</param>
        /// <param name="textureHeight">纹理高度。</param>
        /// <param name="frameWidth">单帧宽度。</param>
        /// <param name="frameHeight">单帧高度。</param>
        /// <param name="columns">每行帧数；<c>&lt;= 0</c> 时按纹理宽度自动推算。</param>
        /// <param name="startIndex">起始帧下标（从 0 开始，按行优先编号）。</param>
        /// <param name="frameCount">取出的帧数；为 <c>null</c> 时取到末尾。</param>
        /// <returns>按行优先顺序排列的源矩形数组。</returns>
        /// <exception cref="ArgumentOutOfRangeException">尺寸非法，或请求的帧范围超出纹理。</exception>
        public static Rectangle[] Slice(
            int textureWidth,
            int textureHeight,
            int frameWidth,
            int frameHeight,
            int columns = 0,
            int startIndex = 0,
            int? frameCount = null)
        {
            if (textureWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(textureWidth), textureWidth, "纹理宽度必须大于 0。");
            if (textureHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(textureHeight), textureHeight, "纹理高度必须大于 0。");
            if (frameWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(frameWidth), frameWidth, "帧宽度必须大于 0。");
            if (frameHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(frameHeight), frameHeight, "帧高度必须大于 0。");
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, "起始下标不能为负数。");

            int actualColumns = columns > 0 ? columns : textureWidth / frameWidth;
            if (actualColumns <= 0)
                throw new ArgumentOutOfRangeException(nameof(columns), columns, "列数必须大于 0。");

            int rows = textureHeight / frameHeight;
            int total = actualColumns * rows;

            int count = frameCount ?? total - startIndex;
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(frameCount), count, "帧数量不能为负数。");
            if (startIndex + count > total)
                throw new ArgumentOutOfRangeException(
                    nameof(frameCount),
                    $"请求的帧范围 [{startIndex}, {startIndex + count}) 超出纹理可容纳的 {total} 帧。");

            var result = new Rectangle[count];
            for (int i = 0; i < count; i++)
            {
                int index = startIndex + i;
                int column = index % actualColumns;
                int row = index / actualColumns;
                int x = column * frameWidth;
                int y = row * frameHeight;

                if (x + frameWidth > textureWidth || y + frameHeight > textureHeight)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(columns),
                        $"第 {index} 帧的矩形 ({x}, {y}, {frameWidth}, {frameHeight}) 超出纹理范围。");
                }

                result[i] = new Rectangle(x, y, frameWidth, frameHeight);
            }

            return result;
        }

        /// <summary>按网格切分指定纹理，返回每个格子的源矩形。</summary>
        /// <param name="texture">要切分的纹理。</param>
        /// <param name="frameWidth">单帧宽度。</param>
        /// <param name="frameHeight">单帧高度。</param>
        /// <param name="columns">每行帧数；<c>&lt;= 0</c> 时自动推算。</param>
        /// <param name="startIndex">起始帧下标。</param>
        /// <param name="frameCount">取出的帧数；为 <c>null</c> 时取到末尾。</param>
        /// <exception cref="ArgumentNullException">纹理为 null。</exception>
        public static Rectangle[] Slice(
            Texture2D texture,
            int frameWidth,
            int frameHeight,
            int columns = 0,
            int startIndex = 0,
            int? frameCount = null)
        {
            if (texture is null) throw new ArgumentNullException(nameof(texture));

            return Slice(texture.Width, texture.Height, frameWidth, frameHeight, columns, startIndex, frameCount);
        }
    }
}
