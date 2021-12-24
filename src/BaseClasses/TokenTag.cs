using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text.Tagging;

namespace BaseClasses
{
    public class TokenTag : ITag
    {
        public TokenTag(object tokenType, bool supportOutlining = false, params string[] errorMessages)
        {
            TokenType = tokenType;
            SupportOutlining = supportOutlining;
            ErrorMessages = errorMessages;
        }

        public virtual object TokenType { get; set; }
        public virtual bool SupportOutlining { get; set; }
        public virtual IList<string> ErrorMessages { get; set; }
        public virtual bool IsValid => ErrorMessages?.Any() == false;
    }
}
