﻿using LobbyControl.Patches;

namespace LobbyControl.TerminalCommands
{
    public class Command
    {
        public TerminalNode previousTerminalNode;

        public bool awaitingConfirmation
        {
            get { return CommandManager.awaitingConfirmationCommand == this; }
            set { CommandManager.awaitingConfirmationCommand = value ? this : null; }
        }

        public virtual bool IsCommand(string[] args)
        {
            return false;
        }

        public virtual TerminalNode Execute(string[] args)
        {
            return TerminalPatch.CreateTerminalNode("Execute override was not found.\n\n");
        }

        public virtual TerminalNode ExecuteConfirmation(string[] args)
        {
            string arg = args[0].ToLower();

            if ("confirm".Contains(arg) && arg.Length > 0)
            {
                return OnConfirm(args);
            }

            if ("deny".Contains(arg) && arg.Length > 0)
            {
                return OnDeny(args);
            }

            return OnInvalidInput(args);
        }

        protected virtual TerminalNode OnConfirm(string[] args)
        {
            return TerminalPatch.CreateTerminalNode("OnConfirm override was not found.\n\n");
        }

        protected virtual TerminalNode OnDeny(string[] args)
        {
            return TerminalPatch.CreateTerminalNode("OnDeny override was not found.\n\n");
        }

        protected virtual TerminalNode OnInvalidInput(string[] args)
        {
            return previousTerminalNode;
        }
    }
}
