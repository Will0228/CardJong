// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// record の init アクセサをコンパイルするために必要な型。
    /// .NET 5 以降には標準で入っているが、Unity が参照する netstandard 2.1 には無いので
    /// 自前で用意する。中身は空でよく、コンパイラが目印として見るだけ。
    /// </summary>
    /// <remarks>
    /// アセンブリごとに必要になる。internal にしているのは、将来 Unity 側が同じ型を
    /// 持ち込んだときに、自分のアセンブリのものが優先されて衝突しないようにするため。
    /// </remarks>
    internal static class IsExternalInit
    {
    }
}
