using System;
using System.Collections.Generic;

namespace Doki.Renpie.RenDisco
{
    public class Play
    {
        private readonly Play _parent; // The current parent
        private readonly IRuntimeEngine _runtime; // The current runtime
        private Play _child = null; // The current child
        private bool _jumped; // If we've been jumped - will mean once the child is done, we end the play
        private bool _waitingForInput; // If this current Play is waiting for input

        public List<RenpyCommand> Commands;
        public int ProgramCounter { get; set; }
        public bool ReturnToParent { get; set; }
        public Play CurrentChild
        {
            get => _child;
            set => _child = value;
        }
        public bool WaitingForInput
        {
            get => _waitingForInput || (_child?.WaitingForInput ?? false);
            set => _waitingForInput = value;
        }


        /// <summary>
        /// Constructor for the play execution environment.
        /// </summary>
        /// <param name="runtime">The engine that executes the script actions.</param>
        /// <param name="commands">List of commands to execute.</param>
        /// <param name="parent">The parent Play context, used for handling scopes and returns.</param>
        public Play(IRuntimeEngine runtime, List<RenpyCommand> commands, Play parent = null)
        {
            Commands = commands;
            ProgramCounter = 0;
            ReturnToParent = false;
            WaitingForInput = false;
            _runtime = runtime;
            _parent = parent;
        }

        /// <summary>
        /// Execute commands from the current ProgramCounter position.
        /// </summary>
        /// <param name="returnToParent">
        /// Specifies if this scope takes responsibility for calling parent context after completing current commands.
        /// </param>
        /// <param name="stepContext">Set our Step context.</param>
        /// <returns>Boolean indicating if execution should continue.</returns>
        public bool Step(bool returnToParent = false, StepContext stepContext = null)
        {
            // This will permanently set current execution to return to Parent.
            if (returnToParent) ReturnToParent = true;

            // If there's a child, this current Play context is responsible for handling its execution.
            if (_child != null)
            {
                if (_child.ProgramCounter >= _child.Commands.Count)
                {
                    // If our child is done, and we don't need to return to Parent, get rid of it.
                    if (!_jumped && !ReturnToParent)
                    {
                        _child = null;
                        ProgramCounter++;
                    }
                    // If we've jumped, we end our execution once our Child is done.
                    else if (_jumped)
                        ProgramCounter = Commands.Count + 1;
                }
                else
                {
                    _child.Step(stepContext: stepContext);
                    return true;
                }
            }

            // Check if we're done here, and if we're responsible for a parent.
            if (ProgramCounter >= Commands.Count)
            {
                // A parent doesn't necessarily mean this play is responsible for it. Check. 
                if (_parent != null && ReturnToParent)
                {
                    _parent.Step(true, stepContext: stepContext);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // Handle the current command
            ExecuteCommand(Commands[ProgramCounter], stepContext);

            // Menus will block further execution.
            if (!WaitingForInput)
            {
                ProgramCounter++;
            }

            return true;
        }

        /// <summary>
        /// Executes a single command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>Boolean indicating if execution should continue.</returns>
        private bool ExecuteCommand(RenpyCommand command, StepContext stepContext = null)
        {
            switch (command)
            {
                case Label label:
                    ExecuteLabel(label, stepContext);
                    break;
                case Dialogue dialogue:
                    if (stepContext != null)
                    {
                        WaitingForInput = false;
                    }
                    else if (!WaitingForInput)
                    {
                        _runtime.ShowDialogue(dialogue.Character, dialogue.Text);
                        WaitingForInput = true;
                    }
                    break;
                case Narration narration:
                    if (stepContext != null)
                    {
                        WaitingForInput = false;
                    }
                    else if (!WaitingForInput)
                    {
                        _runtime.ShowNarration(narration.Text);
                        WaitingForInput = true;
                    }
                    break;
                case Scene scene:
                    _runtime.ShowImage(scene.Image, scene.Transition);
                    break;
                case Show show:
                    _runtime.ShowImage(show.Image, show.Transition);
                    break;
                case Define define:
                    ExecuteDefine(define);
                    break;
                case IfCondition ifCondition:
                    ExecuteConditionalBlock(ifCondition.Condition, ifCondition.Content, stepContext);
                    break;
                case ElifCondition elifCondition:
                    ExecuteConditionalBlock(elifCondition.Condition, elifCondition.Content, stepContext);
                    break;
                case Menu menu:
                    ExecuteMenu(menu, stepContext);
                    break;
                case Jump jump:
                    ExecuteJump(jump.Label, stepContext);
                    break;
                case PlayMusic playMusic:
                    _runtime.PlayMusic(playMusic.File, playMusic.FadeIn);
                    break;
                case Pause pause:
                    _runtime.Pause(pause.Duration);
                    break;
                case StopMusic stopMusic:
                    _runtime.StopMusic(stopMusic.FadeOut);
                    break;
                case Hide hide:
                    _runtime.HideImage(hide.Image, hide.Transition);
                    break;
                default:
                    Console.WriteLine($"Unknown command type encountered: {command.Type}");
                    break;
            }
            return true;
        }

        /// <summary>
        /// Executes defined variables or character settings.
        /// </summary>
        /// <param name="define">The define command to execute.</param>
        private void ExecuteDefine(Define define)
        {
            _runtime.ExecuteDefine(define);
        }

        /// <summary>
        /// Display a menu and handle choice consequences.
        /// </summary>
        /// <param name="menu">The menu command containing choices and responses.</param>
        /// <returns>The result of the run, whether to break or not.</returns>
        private void ExecuteMenu(Menu menu, StepContext stepContext = null)
        {
            try
            {
                if (stepContext?.Choice != null)
                {
                    int selectedChoice = stepContext.Choice ?? -1;
                    if (selectedChoice == -1) return;
                    WaitingForInput = false;
                    _child = new Play(_runtime, menu.Choices[selectedChoice].Response, this);
                    _child.Step(stepContext: stepContext);
                }
                else if (!WaitingForInput)
                {
                    _runtime.ShowChoices(menu.Choices);
                    WaitingForInput = true;
                }
            }
            catch (Exception exception)
            {
                if (exception is ArgumentOutOfRangeException)
                {
                    Console.WriteLine($"Unknown dialogue option: {stepContext?.Choice}");
                    return;
                }

                Console.WriteLine($"Unhandled Exception: {exception.StackTrace}");
            }
        }

        /// <summary>
        /// Handle commands conditionally.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="content"></param>
        private void ExecuteLabel(Label label, StepContext stepContext = null)
        {
            _child = new Play(_runtime, label.Commands, this);
            _child.Step(stepContext: stepContext);
        }

        /// <summary>
        /// Handle commands conditionally.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="content"></param>
        /// <returns>The result of the run, whether to break or not.</returns>
        private void ExecuteConditionalBlock(string condition, List<RenpyCommand> content, StepContext stepContext = null)
        {
            if (EvaluateCondition(condition))
            {
                _child = new Play(_runtime, content, this);
                _child.Step(stepContext: stepContext);
            }
        }

        /// <summary>
        /// Execute a jump command which modifies the program counter.
        /// </summary>
        /// <param name="labelName">The label name to jump to.</param>
        private void ExecuteJump(string labelName, StepContext stepContext = null)
        {
            _child = FindLabel(labelName);
            if (_child != null)
            {
                _jumped = true;
                _child.CurrentChild = null;
                _child?.Step(returnToParent: true, stepContext: stepContext);
            }
        }

        /// <summary>
        /// Locate a label within the command set.
        /// </summary>
        /// <param name="labelName">The name of the label to find.</param>
        /// <returns>The Play instance associated with the found label, or null if no label found.</returns>
        private Play FindLabel(string labelName)
        {
            for (var i = 0; i < Commands.Count; i++)
            {
                if (Commands[i] is Label label && label.Name == labelName)
                {
                    ProgramCounter = i;
                    return this;
                }
            }
            return _parent?.FindLabel(labelName);
        }

        /// <summary>
        /// Evaluate a boolean condition.
        /// </summary>
        /// <param name="condition">The condition as a string.</param>
        /// <returns>The result of the condition evaluation.</returns>
        private bool EvaluateCondition(string condition)
        {
            return Evaluate.EvaluateCondition(_runtime, condition);
        }
    }
}