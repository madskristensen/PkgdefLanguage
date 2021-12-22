using System.Threading.Tasks;

namespace PkgdefLanguage.Test
{
    public static class Extensions
    {
        public static async Task WaitForParsingCompleteAsync(this Document document)
        {
            while (document.IsProcessing)
            {
                await Task.Delay(2);
            }
        }
    }
}
