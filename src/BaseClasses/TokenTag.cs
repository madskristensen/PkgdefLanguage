using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace BaseClasses
{
    public class TokenTag : ITag
    {
        public TokenTag(object tokenType, bool supportOutlining, Func<SnapshotPoint, Task<object>> getTooltipAsync, params string[] errorMessages)
        {
            TokenType = tokenType;
            SupportOutlining = supportOutlining;
            GetTooltipAsync = getTooltipAsync;
            ErrorMessages = errorMessages;
        }

        public TokenTag(object tokenType)
            : this(tokenType, false, null)
        { }

        public TokenTag(object tokenType, bool supportOutlining)
            : this(tokenType, supportOutlining, null)
        { }

        public virtual object TokenType { get; set; }
        public virtual bool SupportOutlining { get; set; }
        public virtual IList<string> ErrorMessages { get; set; }
        public virtual bool IsValid => ErrorMessages?.Any() == false;
        public virtual Func<SnapshotPoint, Task<object>> GetTooltipAsync { get; set; }
    }
}
