﻿using System.Collections;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

public class mashematicsScript : MonoBehaviour {

    public KMAudio Audio;

    public KMSelectable SubmitBtn;
    public KMSelectable PushBtn;

    public TextMesh mathProblem1;
    public TextMesh mathProblem2;
    public TextMesh mathProblem3;
    public TextMesh indicatorTxt;

    public KMBombModule Module;

    public TextMesh mathProblemOp1;
    public TextMesh mathProblemOp2;


    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;
    private Number number;

    private int numberOfPush;

    private bool isSolved;

    private class Number
    {
        private readonly int moduleId;

        public Number(int moduleId)
        {
            this.moduleId = moduleId;
            this.Number1 = Random.Range(0, 100);
            this.Number2 = Random.Range(0, 100);
            this.Number3 = Random.Range(0, 100);
            this.Operator1 = Random.Range(0, 3);
            this.Operator2 = Random.Range(0, 3);
        }

        private int GetAnswer()
        {
            if (IsMultiply(this.Operator2))
            {
                return PerformOperation(this.Number1, PerformOperation(this.Number2, this.Number3, this.Operator2), this.Operator1);
            }

            return PerformOperation(PerformOperation(this.Number1, this.Number2, this.Operator1), this.Number3, this.Operator2);
        }

        public int GetNumberOfRequiredPush()
        {
            var answer = this.GetAnswer();
            if (answer < 0)
            {
                while (answer < 0)
                {
                    answer += 50;
                }
            }
            else if (answer >= 100)
            {
                while (answer >= 100)
                {
                    answer -= 50;
                }
            }

            return answer;
        }
        public int Number1
        {
            get;
            private set;
        }

        public int Number2
        {
            get;
            private set;
        }

        public int Number3
        {
            get;
            private set;
        }

        public string OperatorType1
        {
            get
            {
                return OperatorType(this.Operator1);
            }
        }

        public string OperatorType2
        {
            get
            {
                return OperatorType(this.Operator2);
            }
        }

        public override string ToString()
        {
            return string.Format("[Mashematics #{7}] Math problem: {0} {1} {2} {3} {4} = {5} (requires push {6})", 
                this.Number1, this.OperatorType1, this.Number2, this.OperatorType2, this.Number3,
                this.GetAnswer(), this.GetNumberOfRequiredPush(), this.moduleId);
        }

        private int Operator1 { get; set; }

        private int Operator2 { get; set; }

        private static int PerformOperation(int first, int second, int op)
        {
            if (IsPlus(op))
                return first + second;
            if (IsMinus(op))
                return first - second;

            return first * second;

        }

        private static bool IsPlus(int op)
        {
            return op == 0;
        }

        private static bool IsMinus(int op)
        {
            return op == 1;
        }

        private static bool IsMultiply(int op)
        {
            return op == 2;
        }

        private static string OperatorType(int op)
        {
            return IsPlus(op) ? "+" : (IsMinus(op) ? "-" : "×");
        }

    }

    void Activate()
    {
        number = new Number(this._moduleId);
        Debug.Log(number.ToString());

        PushBtn.OnInteract += delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.PushBtn.transform);
            PushBtn.AddInteractionPunch();
            if (this.isSolved)
                return false;
            {
                this.numberOfPush++;
                this.indicatorTxt.text = this.numberOfPush.ToString("D2");
                if (this.numberOfPush == 100)
                {
                    Module.HandleStrike();
                    this.numberOfPush = 0;
                    this.indicatorTxt.text = this.numberOfPush.ToString("D2");

                }
            }

            return false;
        };

        SubmitBtn.OnInteract += delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.SubmitBtn.transform);
            SubmitBtn.AddInteractionPunch();
            Debug.LogFormat("[Mashematics #{1}] Submitting {0}", this.numberOfPush, _moduleId);
            if (this.isSolved)
                return false;

            {
                if (this.numberOfPush == number.GetNumberOfRequiredPush())
                {
                    Debug.LogFormat("[Mashematics #{0}] Module disarmed.", _moduleId);
                    mathProblem1.text = "";
                    mathProblem2.text = "";
                    mathProblem3.text = "";
                    indicatorTxt.text = "";
                    mathProblemOp1.text = "";
                    mathProblemOp2.text = "";
                    this.isSolved = true;
                    Module.HandlePass();
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, this.SubmitBtn.transform);
                }
                else
                {
                    Debug.LogFormat("[Mashematics #{0}] Strike!", _moduleId);
                    Module.HandleStrike();
                    this.numberOfPush = 0;
                    this.indicatorTxt.text = this.numberOfPush.ToString("D2");
                }
            }

            return false;
        };

        this.mathProblemOp1.text = number.OperatorType1;
        this.mathProblemOp2.text = number.OperatorType2;
        this.mathProblem1.text = number.Number1.ToString();
        this.mathProblem2.text = number.Number2.ToString();
        this.mathProblem3.text = number.Number3.ToString();
        this.indicatorTxt.text = this.numberOfPush.ToString("D2");
    }

    // Use this for initialization
    void Start ()
    {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
    }
    
    public IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        Match m;
        if ((m = Regex.Match(command, @"^submit +(\d+)$")).Success)
        {
            int value;
            if(!int.TryParse(m.Groups[1].Value, out value))
            {
                yield return "sendtochaterror Stop trying to break the module Kappa";
                yield break;
            }
            yield return null;
            if (value < numberOfPush)
            {
                yield return "sendtochaterror The number you are trying to submit is smaller than the current number.";
                yield break;
            }
            yield return Enumerable.Repeat(PushBtn, value - numberOfPush).Concat(new[] { SubmitBtn });
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        var answer = number.GetNumberOfRequiredPush();
        if (numberOfPush > answer)
        {
            numberOfPush = 0;
        }

        var noPush = Math.Abs(answer - numberOfPush);
        for (var i = 0; i < noPush; i++)
        {
            PushBtn.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        SubmitBtn.OnInteract();
    }

    public const string TwitchHelpMessage = "Submit the correct answer using !{0} submit ##.";
}
