using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ToolboxAnimWindow : EditorWindow
{
	private int tabIndex;
	private string[] tabs = new string[] { "Animator", "Animation" };

	private List<Animator> animatorList = new List<Animator>();
	private List<bool> animtorButtonList = new List<bool>();

	private Animator currentAnimator;
	private List<AnimationClip> animationClips = new List<AnimationClip>();
	private List<bool> animationClipsButtonList = new List<bool>();
	private int currentAnimationClipIndex;
	private bool isPlaying = false;

	private float animationSpeed = 1;
	private float previousAnimTime;
	private float animTime;

	private float elapsedTime;
	private float delay;

	private float editorLastTime;

	private PlayModeStateChange StateUnity;

	[MenuItem("Toolbox/Toolbox Window")]
	static void InitWindow()
	{
		EditorWindow window = GetWindow<ToolboxAnimWindow>();
		window.autoRepaintOnSceneChange = true;
		window.Show();
		window.titleContent = new GUIContent("Toolbox Anim Window");
	}

	private void OnGUI()
	{
		tabIndex = GUILayout.Toolbar(tabIndex, tabs);
		GUILayout.Space(10f);

		EditorApplication.playModeStateChanged += StopAnimationSwitchMod;

		switch (StateUnity)
		{
			case PlayModeStateChange.EnteredEditMode:
				switch (tabIndex)
				{
					case 0: GUIAnimatorSelect(); break;
					case 1: if (currentAnimator != null) GUIAnimationDisplay(); break;
				}
				break;
			case PlayModeStateChange.ExitingEditMode:
				EditorGUILayout.HelpBox("You cannot use this tool in PlayMode", MessageType.Warning);
				break;
			case PlayModeStateChange.EnteredPlayMode:
				EditorGUILayout.HelpBox("You cannot use this tool in PlayMode", MessageType.Warning);
				break;
			default:
				throw new System.InvalidOperationException($"{StateUnity} is not part of PlayModeStateChange");
		}
	}

	private void OnEnable()
	{
		if (Application.isPlaying)
			StateUnity = PlayModeStateChange.EnteredPlayMode;
	}

	private void StopAnimationSwitchMod(PlayModeStateChange state)
	{
		if (state == PlayModeStateChange.EnteredPlayMode && isPlaying)
			StopAnim();
		if (state == PlayModeStateChange.ExitingPlayMode && isPlaying)
			StopAnim();
		StateUnity = state;
	}

	private void GUIAnimatorSelect()
	{
		FindAnimatorInScene();
		CheckButtons();
	}

	private void GUIAnimationDisplay()
	{
		DisplayAnimation();
	}

	#region Animator Selection
	private void FindAnimatorInScene()
	{
		animatorList.Clear();
		animtorButtonList.Clear();

		foreach (GameObject rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
		{
			Animator a = rootGameObject.GetComponentInChildren<Animator>();
			if(a != null)
				animatorList.Add(a);
		}

		if (animatorList.Count == 0)
			return;

		foreach (Animator a in animatorList)
		{
			if(a.gameObject != null)
			animtorButtonList.Add(GUILayout.Button($"Select {a.gameObject.name} animator"));
		}
	}

	private void CheckButtons()
	{
		for (int i = 0; i < animatorList.Count; i++)
		{
			if (animtorButtonList[i] && animatorList[i] != null)
			{
				StopAnim();
				Selection.activeGameObject = animatorList[i].gameObject;
				SceneView.lastActiveSceneView.FrameSelected();
				EditorGUIUtility.PingObject(animatorList[i].gameObject);
				currentAnimator = animatorList[i];
			}
		}
	}
	#endregion

	#region animation
	private void DisplayAnimation()
	{
		animationClips = FindAnimClips(currentAnimator);

		for (int i = 0; i < animationClips.Count; i++)
		{
			if (GUILayout.Button($"{animationClips[i].name} animation"))
			{
				StopAnim();
				currentAnimationClipIndex = i;
				PlayAnim();
			};
		}

		animationSpeed = EditorGUILayout.Slider("Animation Speed", animationSpeed, 0f, 5f);
		delay = EditorGUILayout.Slider("Delay Between Animation", delay, 0f, 10f);
		EditorGUILayout.FloatField("Animation Time", animTime % animationClips[currentAnimationClipIndex].length);
	}

	private void PlayAnim()
	{
		if (isPlaying) return;
		editorLastTime = Time.realtimeSinceStartup;
		EditorApplication.update += _OnEditorUpdate;
		AnimationMode.StartAnimationMode();
		isPlaying = true;
	}

	private void StopAnim()
	{
		if (!isPlaying) return;
		EditorApplication.update -= _OnEditorUpdate;
		AnimationMode.StopAnimationMode();
		isPlaying = false;
	}

	private bool isAwaiting;
	private void _OnEditorUpdate()
	{
		if (!isAwaiting)
		{
			animTime = (Time.realtimeSinceStartup - editorLastTime) * animationSpeed;

			if (animTime % animationClips[currentAnimationClipIndex].length < previousAnimTime % animationClips[currentAnimationClipIndex].length && delay != 0.0f)
			{
				isAwaiting = true;
				return;
			}

			IncreaseTimeForAnimation();
			previousAnimTime = animTime;
		}
	}

	private void Update() 
	{
		if (isAwaiting)
			elapsedTime += Time.deltaTime;
		if(elapsedTime >= delay && isAwaiting)
		{
			elapsedTime = 0;
			previousAnimTime = 0;
			editorLastTime = Time.realtimeSinceStartup;
			isAwaiting = false;
		}
	}

	private void IncreaseTimeForAnimation()
	{
		AnimationClip animclip = animationClips[currentAnimationClipIndex];
		previousAnimTime %= animclip.length;
		AnimationMode.SampleAnimationClip(currentAnimator.gameObject, animclip, previousAnimTime);
	}

	private List<AnimationClip> FindAnimClips(Animator animator)
	{
		List<AnimationClip> resultList = new List<AnimationClip>();

		AnimatorController editorController = animator?.runtimeAnimatorController as AnimatorController;

		AnimatorControllerLayer controllerLayer = editorController.layers[0];
		foreach (ChildAnimatorState childState in controllerLayer.stateMachine.states)
		{
			AnimationClip animClip = childState.state.motion as AnimationClip;
			if (animClip != null)
			{
				resultList.Add(animClip);
			}
		}

		return resultList;
	}
	#endregion
}