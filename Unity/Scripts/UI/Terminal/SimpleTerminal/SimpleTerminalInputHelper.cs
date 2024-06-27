﻿using System;
using UnityEngine;

namespace UI.Terminal.SimpleTerminal
{
	public class SimpleTerminalInputHelper
	{
		private readonly ITerminalSounds sounds;
		private readonly SimpleTerminal term;
		private readonly Action<string> Write;

		public SimpleTerminalInputHelper(ITerminalSounds sounds, SimpleTerminal term, Action<string> writeMethod)
		{
			this.sounds = sounds;
			this.term = term;
			this.Write = writeMethod;
		}

		public void Delete()
		{
			Write("\x7f");
		}

		public void UpArrow()
		{
			this.Write("\x1b[1A");
		}

		public void DownArrow()
		{
			this.Write("\x1b[1B");
		}

		public void RightArrow()
		{
			this.Write("\x1b[1C");
		}

		public void LeftArrow()
		{
			this.Write("\x1b[1D");
		}
		
		public void Backspace()
		{
			Char('\b');
		}

		public void Enter()
		{
			// We only write a CR, even on POSIX systems, since this maps to Enter in the line editor. Writing
			// the LF breaks line editor.
			this.Write("\r");
		}

		public void Char(char ch)
		{
			if (ch != '\r' && ch != '\n')
				sounds.PlayTypingSound();
			
			this.Write(ch.ToString());
		}

		public void Raw(KeyCode keyCode, bool modifierControl, bool modifierAlt, bool modifierShift)
		{
			// Fix modifiers for keystrokes where the main key is the same as the modifier
			if (keyCode == KeyCode.LeftControl || keyCode == KeyCode.RightControl)
				modifierControl = false;

			if (keyCode == KeyCode.LeftShift || keyCode == KeyCode.RightShift)
				modifierShift = false;

			if (keyCode == KeyCode.LeftAlt || keyCode == KeyCode.RightAlt)
				modifierAlt = false;
			
			
			var modifierMask = 0;

			if (modifierControl)
				modifierMask += 1;

			if (modifierAlt)
				modifierMask += 2;

			if (modifierShift)
				modifierMask += 4;

			if (modifierMask == 0)
			{
				switch (keyCode)
				{
					case KeyCode.LeftArrow:
						this.LeftArrow();
						return;
					case KeyCode.RightArrow:
						this.RightArrow();
						return;
					case KeyCode.UpArrow:
						this.UpArrow();
						return;
					case KeyCode.DownArrow:
						this.DownArrow();
						return;
					case KeyCode.Backspace:
						this.Backspace();
						return;
					case KeyCode.KeypadEnter:
					case KeyCode.Return:
						this.Enter();
						return;
					case KeyCode.Delete:
						this.Delete();
						return;
				}
			}
			
			// Anything else is written as an escape sequence with a tilde terminator.
			this.Char('\x1b');
			this.Char('[');

			var rawKeyCode = (int) keyCode;
			this.Write(rawKeyCode.ToString());
			this.Write(";");
			this.Write(modifierMask.ToString());
			
			this.Write("~");
		}
	}

	public enum MouseButton
	{
		Left,
		Middle,
		Right
	}
}