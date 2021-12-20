using System.Threading.Tasks;

namespace PkgdefLanguage.Test
{
    public static class Extensions
    {
        public static async Task WaitForParsingCompleteAsync(this Document document)
        {
            while (document.IsParsing)
            {
                await Task.Delay(2);
            }
        }
    }
}
