namespace ReluctersteSDK.MathTool.QuickRandom
{
    /// <summary>
    /// 矩阵随机生成器 —— 提供各类矩阵/多维数组的随机生成方法。
    /// </summary>
    /// <remarks>
    /// <para>所有方法通过 <see cref="ThreadLocal{Random}"/> 保证线程安全。</para>
    /// <para>基础随机矩阵内部复用 <see cref="BasicRandomGen.Next{T}"/>；
    /// 伯努利矩阵内部复用 <see cref="DistributionRandomGen.NextBernoulli"/>。</para>
    /// </remarks>
    public static class MatrixRandomGen
    {
        // ========== ThreadLocal 保证线程安全 ==========
        private static int _seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Rng = new ThreadLocal<Random>(() =>
        {
            int seed = Interlocked.Increment(ref _seed);
            return new Random(seed);
        });

        #region 基础随机矩阵

        /// <summary>
        /// 生成 rows × cols 的二维随机矩阵，每个元素在 [min, max] 范围内均匀分布。
        /// 精度规则与 <see cref="BasicRandomGen.Next{T}"/> 一致。
        /// </summary>
        /// <typeparam name="T">元素类型（值类型，如 int、float、double 等）</typeparam>
        /// <param name="rows">行数</param>
        /// <param name="cols">列数</param>
        /// <param name="min">下限（包含）</param>
        /// <param name="max">上限（包含）</param>
        /// <returns>rows × cols 的二维随机矩阵</returns>
        public static T[,] NextMatrix<T>(int rows, int cols, T min, T max)
            where T : struct, IComparable, IConvertible
        {
            if (rows < 0)
                throw new ArgumentOutOfRangeException(nameof(rows), "行数不能为负数");
            if (cols < 0)
                throw new ArgumentOutOfRangeException(nameof(cols), "列数不能为负数");

            T[,] matrix = new T[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    matrix[i, j] = BasicRandomGen.Next(min, max);
            return matrix;
        }

        /// <summary>
        /// [DI容器重载] 生成任意维数的随机数组，每个元素在 [min, max] 范围内均匀分布。
        /// 通过 <c>params int[] dimensions</c> 灵活指定各维度大小，可生成二维矩阵、三维张量及更高维数组。
        /// </summary>
        /// <typeparam name="T">元素类型（值类型）</typeparam>
        /// <param name="min">下限（包含）</param>
        /// <param name="max">上限（包含）</param>
        /// <param name="dimensions">
        /// 各维度大小。例如：
        /// <c>new[] { 3, 4 }</c> 生成 3×4 矩阵，
        /// <c>new[] { 2, 3, 4 }</c> 生成 2×3×4 三维数组，
        /// <c>new[] { 5, 6, 7, 8 }</c> 生成 5×6×7×8 四维数组
        /// </param>
        /// <returns>多维 <see cref="Array"/>，元素类型为 T，可通过强制转换获取强类型引用</returns>
        /// <example>
        /// <code>
        /// // 生成 3×4×5 的三维随机数组
        /// Array arr = MatrixRandomGen.NextNDArray(0.0, 1.0, 3, 4, 5);
        /// double[,,] tensor = (double[,,])arr;
        /// </code>
        /// </example>
        public static Array NextNDArray<T>(T min, T max, params int[] dimensions)
            where T : struct, IComparable, IConvertible
        {
            if (dimensions == null || dimensions.Length == 0)
                throw new ArgumentException("必须至少指定一个维度大小", nameof(dimensions));
            for (int d = 0; d < dimensions.Length; d++)
                if (dimensions[d] < 0)
                    throw new ArgumentOutOfRangeException(nameof(dimensions), $"维度 dimensions[{d}] 不能为负数");

            Array result = Array.CreateInstance(typeof(T), dimensions);
            FillNDArrayRecursive(result, min, max, new int[dimensions.Length], 0);
            return result;
        }

        #endregion

        #region 随机单位矩阵

        /// <summary>
        /// 生成 n×n 的标准单位矩阵，对角线元素为 1，其余元素为 0。
        /// </summary>
        /// <typeparam name="T">元素类型（值类型）</typeparam>
        /// <param name="n">矩阵阶数</param>
        /// <returns>n×n 单位矩阵</returns>
        public static T[,] NextIdentityMatrix<T>(int n)
            where T : struct, IComparable, IConvertible
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "阶数 n 不能为负数");

            T[,] matrix = new T[n, n];
            T one = (T)Convert.ChangeType(1, typeof(T));
            for (int i = 0; i < n; i++)
                matrix[i, i] = one;
            // 非对角线元素保持 default(T)，对数值类型即为 0
            return matrix;
        }

        /// <summary>
        /// 生成 n×n 的随机对角矩阵：对角线元素在 [diagMin, diagMax] 范围内随机均匀采样，
        /// 非对角线元素保持 default(T)（对数值类型即为 0）。
        /// </summary>
        /// <typeparam name="T">元素类型（值类型）</typeparam>
        /// <param name="n">矩阵阶数</param>
        /// <param name="diagMin">对角线元素下限（包含）</param>
        /// <param name="diagMax">对角线元素上限（包含）</param>
        /// <returns>n×n 随机对角矩阵</returns>
        public static T[,] NextIdentityMatrix<T>(int n, T diagMin, T diagMax)
            where T : struct, IComparable, IConvertible
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "阶数 n 不能为负数");

            T[,] matrix = new T[n, n];
            for (int i = 0; i < n; i++)
                matrix[i, i] = BasicRandomGen.Next(diagMin, diagMax);
            return matrix;
        }

        #endregion

        #region 随机邻接矩阵

        /// <summary>
        /// 生成 n×n 的随机邻接矩阵（图论）。
        /// 每个可能的边以概率 <paramref name="edgeProbability"/> 存在。
        /// 无向图模式下矩阵对称；有向图模式下每个方向独立采样。
        /// </summary>
        /// <param name="n">顶点数量（矩阵阶数）</param>
        /// <param name="edgeProbability">
        /// 每条边存在的概率，范围 [0, 1]。默认 0.5。
        /// </param>
        /// <param name="directed">
        /// 是否为有向图。false 表示无向图（矩阵对称），true 表示有向图（非对称）。
        /// 默认 false。
        /// </param>
        /// <param name="allowSelfLoops">
        /// 是否允许自环（对角线为 1）。默认 false，对角线恒为 0。
        /// </param>
        /// <returns>n×n 的 0/1 邻接矩阵</returns>
        public static int[,] NextAdjacencyMatrix(int n, double edgeProbability = 0.5,
            bool directed = false, bool allowSelfLoops = false)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "顶点数 n 不能为负数");
            if (edgeProbability < 0 || edgeProbability > 1)
                throw new ArgumentOutOfRangeException(nameof(edgeProbability), "概率必须在 [0, 1] 范围内");

            int[,] matrix = new int[n, n];

            if (directed)
            {
                // 有向图：每个元素独立采样
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j && !allowSelfLoops)
                            continue; // 保持 0
                        matrix[i, j] = DistributionRandomGen.NextBernoulli(edgeProbability);
                    }
                }
            }
            else
            {
                // 无向图：只采样上三角，镜像到下三角
                for (int i = 0; i < n; i++)
                {
                    for (int j = i; j < n; j++)
                    {
                        if (i == j)
                        {
                            if (allowSelfLoops)
                                matrix[i, i] = DistributionRandomGen.NextBernoulli(edgeProbability);
                            continue;
                        }
                        int edge = DistributionRandomGen.NextBernoulli(edgeProbability);
                        matrix[i, j] = edge;
                        matrix[j, i] = edge;
                    }
                }
            }

            return matrix;
        }

        #endregion

        #region 随机伯努利分布矩阵

        /// <summary>
        /// 生成 rows × cols 的伯努利分布矩阵，每个元素独立服从 Bernoulli(p)：
        /// 以概率 p 取值为 1，以概率 1-p 取值为 0。
        /// 内部复用 <see cref="DistributionRandomGen.NextBernoulli"/>。
        /// </summary>
        /// <param name="rows">行数</param>
        /// <param name="cols">列数</param>
        /// <param name="p">每个元素为 1 的概率，范围 [0, 1]</param>
        /// <returns>rows × cols 的 0/1 伯努利矩阵</returns>
        public static int[,] NextBernoulliMatrix(int rows, int cols, double p)
        {
            if (rows < 0)
                throw new ArgumentOutOfRangeException(nameof(rows), "行数不能为负数");
            if (cols < 0)
                throw new ArgumentOutOfRangeException(nameof(cols), "列数不能为负数");
            if (p < 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 [0, 1] 范围内");

            int[,] matrix = new int[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    matrix[i, j] = DistributionRandomGen.NextBernoulli(p);
            return matrix;
        }

        #endregion

        #region 内部辅助方法

        /// <summary>
        /// 递归填充多维数组的每个元素。
        /// </summary>
        private static void FillNDArrayRecursive<T>(Array array, T min, T max,
            int[] indices, int dimension)
            where T : struct, IComparable, IConvertible
        {
            int length = array.GetLength(dimension);
            if (dimension == array.Rank - 1)
            {
                // 最内层维度：直接遍历赋值
                for (int i = 0; i < length; i++)
                {
                    indices[dimension] = i;
                    array.SetValue(BasicRandomGen.Next(min, max), indices);
                }
            }
            else
            {
                // 外层维度：递归进入下一维度
                for (int i = 0; i < length; i++)
                {
                    indices[dimension] = i;
                    FillNDArrayRecursive(array, min, max, indices, dimension + 1);
                }
            }
        }

        #endregion
    }
}
