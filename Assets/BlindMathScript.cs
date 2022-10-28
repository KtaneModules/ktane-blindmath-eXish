using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class BlindMathScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;
    public TextMesh display;

    private string[] selectedOperations = new string[15];
    private List<int> pressedBtns = new List<int>();
    private List<int> solutionBtns = new List<int>();
    private int target;
    private int current;
    private int initial;
    private bool activated;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += Activate;
    }

    void Start()
    {
        redo:
        for (int i = 0; i < selectedOperations.Length; i++)
        {
            int type = UnityEngine.Random.Range(0, 4);
            switch (type)
            {
                case 0:
                    selectedOperations[i] = "+" + UnityEngine.Random.Range(1, 101);
                    break;
                case 1:
                    selectedOperations[i] = "-" + UnityEngine.Random.Range(1, 101);
                    break;
                case 2:
                    int choice = UnityEngine.Random.Range(-5, 6);
                    while (choice == 0 || choice == 1)
                        choice = UnityEngine.Random.Range(-5, 6);
                    selectedOperations[i] = "×" + choice;
                    break;
                default:
                    choice = UnityEngine.Random.Range(-5, 6);
                    while (choice == 0 || choice == 1)
                        choice = UnityEngine.Random.Range(-5, 6);
                    selectedOperations[i] = "÷" + choice;
                    break;
            }
        }
        string targetBuild = "-";
        targetBuild += bomb.GetBatteryCount();
        targetBuild += bomb.GetIndicators().Count();
        targetBuild += bomb.GetPortCount();
        targetBuild += bomb.GetSerialNumberNumbers().Last();
        target = int.Parse(targetBuild);
        if (bomb.GetSerialNumberLetters().Any(x => x.EqualsAny('A', 'E', 'I', 'O', 'U')))
            target *= -1;
        redo2:
        solutionBtns = Enumerable.Range(1, 15).ToList();
        for (int i = 0; i < solutionBtns.Count; i++)
        {
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                solutionBtns.RemoveAt(i);
                i--;
            }
        }
        if (solutionBtns.Count == 0)
            goto redo2;
        solutionBtns = solutionBtns.Shuffle();
        int temp = target;
        for (int i = solutionBtns.Count - 1; i >= 0; i--)
        {
            string newOp = selectedOperations[solutionBtns[i] - 1];
            if (newOp.Contains("÷"))
                newOp = newOp.Replace("÷", "×");
            else if (newOp.Contains("×"))
                newOp = newOp.Replace("×", "÷");
            else if (newOp.Contains("+"))
                newOp = newOp.Replace("+", "-");
            else if (newOp.Contains("-"))
                newOp = newOp.Replace("-", "+");
            temp = ApplyOperation(newOp, temp);
        }
        initial = temp;
        current = initial;
        for (int i = 0; i < solutionBtns.Count; i++)
            temp = ApplyOperation(selectedOperations[solutionBtns[i] - 1], temp);
        if (temp != target)
            goto redo;
        Debug.LogFormat("[Blind Math #{0}] Blank button operations: {1}.", moduleId, selectedOperations.Join(", "));
        Debug.LogFormat("[Blind Math #{0}] Target number: {1}.", moduleId, target);
        Debug.LogFormat("[Blind Math #{0}] Initial number: {1}.", moduleId, initial);
        Debug.LogFormat("[Blind Math #{0}] One way to get to the target is by pressing: {1}.", moduleId, solutionBtns.Join(", "));
    }

    void Activate()
    {
        display.text = initial.ToString();
        if (display.text.Length > 6)
            display.transform.localScale = new Vector3(0.00036f - (.00004f * (display.text.Length - 6)), 0.001f, 1);
        activated = true;
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && activated != false)
        {
            int index = Array.IndexOf(buttons, pressed);
            if (index == 15)
            {
                pressed.AddInteractionPunch();
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                if (current == target)
                {
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    Debug.LogFormat("[Blind Math #{0}] Submit pressed, module solved!", moduleId);
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Blind Math #{0}] Submit pressed, strike!", moduleId);
                }
            }
            else if (index == 16)
            {
                pressed.AddInteractionPunch();
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                current = initial;
                Debug.LogFormat("[Blind Math #{0}] Reset pressed.", moduleId);
                display.text = current.ToString();
                if (display.text.Length > 6)
                    display.transform.localScale = new Vector3(0.00036f - (.00004f * (display.text.Length - 6)), 0.001f, 1);
                else
                    display.transform.localScale = new Vector3(0.00036f, 0.001f, 1);
                pressedBtns = new List<int>();
            }
            else if (!pressedBtns.Contains(index))
            {
                pressed.AddInteractionPunch();
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                current = ApplyOperation(selectedOperations[index], current);
                Debug.LogFormat("[Blind Math #{0}] Pressed blank button {1}, current display is {2}.", moduleId, index + 1, current);
                display.text = current.ToString();
                if (display.text.Length > 6)
                    display.transform.localScale = new Vector3(0.00036f - (.00004f * (display.text.Length - 6)), 0.001f, 1);
                else
                    display.transform.localScale = new Vector3(0.00036f, 0.001f, 1);
                pressedBtns.Add(index);
            }
        }
    }

    int ApplyOperation(string op, int num)
    {
        switch (op.First())
        {
            case '+':
                return num + int.Parse(op.Substring(1));
            case '-':
                return num - int.Parse(op.Substring(1));
            case '×':
                return num * int.Parse(op.Substring(1));
            default:
                return num / int.Parse(op.Substring(1));
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <p1> (p2)... [Presses the blank button(s) in the specified position(s)] | !{0} press <submit/green/reset/red> [Presses the green ""S"" or red ""R"" button] | Valid positions are 1-15 in reading order";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify something to press!";
            else
            {
                List<KMSelectable> btns = new List<KMSelectable>();
                for (int i = 1; i < parameters.Length; i++)
                {
                    if (parameters[i].ToUpperInvariant().EqualsAny("SUBMIT", "GREEN"))
                        btns.Add(buttons[15]);
                    else if (parameters[i].ToUpperInvariant().EqualsAny("RESET", "RED"))
                        btns.Add(buttons[16]);
                    else if (parameters[i].EqualsAny("1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"))
                        btns.Add(buttons[int.Parse(parameters[i]) - 1]);
                    else
                    {
                        yield return "sendtochaterror!f '" + parameters[i] + "' is an invalid parameter!";
                        yield break;
                    }
                }
                yield return null;
                yield return btns;
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!activated) yield return true;
        if (pressedBtns.Count <= solutionBtns.Count)
        {
            for (int i = 0; i < pressedBtns.Count; i++)
            {
                if (pressedBtns[i] != solutionBtns[i] - 1)
                {
                    buttons[16].OnInteract();
                    yield return new WaitForSeconds(.1f);
                    break;
                }
            }
        }
        else
        {
            buttons[16].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        int start = pressedBtns.Count;
        for (int i = start; i < solutionBtns.Count; i++)
        {
            buttons[solutionBtns[i] - 1].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        buttons[15].OnInteract();
    }
}