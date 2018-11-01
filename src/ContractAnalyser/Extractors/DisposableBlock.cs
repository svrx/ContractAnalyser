using System;

namespace ContractAnalyser.Extractors
{
    internal class DisposableBlock : IDisposable
    {
        private readonly Action enterAction;
        private readonly Action exitAction;

        public DisposableBlock(Action enterAction, Action exitAction)
        {
            this.enterAction = enterAction ?? throw new ArgumentNullException(nameof(enterAction));
            this.exitAction = exitAction ?? throw new ArgumentNullException(nameof(exitAction));

            enterAction();
        }

        public void Dispose()
        {
            exitAction();
        }
    }
}
