﻿#nullable enable

using System;
using System.Collections.Generic;
using CustomInput.InputManagers;
using Player;
using UI.Windowing;
using UnityEngine;
using UnityEngine.Serialization;
using UnityExtensions;
using Utility;

namespace CustomInput
{
	public class GlobalInputManager : MonoBehaviour
	{
		private GameControls gameControls = null!;
		private readonly List<IInputManager> inputManagers = new List<IInputManager>();
		
		[Header("Dependencies")]
		[SerializeField]
		private PlayerInstanceHolder playerHolder = null!;

		[FormerlySerializedAs("windowDragService")]
		[SerializeField]
		private WindowFocusService windowFocusService = null!;

		private void Awake()
		{
			this.AssertAllFieldsAreSerialized(typeof(GlobalInputManager));

			gameControls = new GameControls();
		}

		private void Start()
		{
			inputManagers.Insert(0, new WindowInputManager(windowFocusService));
			inputManagers.Insert(1, new ShellInputManager());
		}

		private void OnEnable()
		{
			gameControls.Enable();
		}

		private void OnDisable()
		{
			gameControls.Disable();
		}

		private void Update()
		{
			var consumed = false;
			PlayerInstance playerInstance = playerHolder.Value;

			for (var i = 0; i < inputManagers.Count; i++)
			{
				IInputManager inputManager = inputManagers[i];
				consumed |= inputManager.HandleInputs(playerInstance, gameControls, consumed);
			}
		}
	}
}