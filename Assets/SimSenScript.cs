using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class SimSenScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMColorblindMode cbmode;
    public KMSelectable modSelect;
    public List<KMSelectable> buttons;
    public List<KMSelectable> mazewalls;
    public GameObject[] mazes;
    public Renderer[] mazereveal;
    public Renderer[] bulbs;
    public Renderer[] filaments;
    public Light[] lights;
    public Material[] bulbcols;
    public Material[] io;
    public TextMesh cbtext;
    public Collider[] colliders;

    private bool cb;
    private readonly Color32[] cols = new Color32[8] { new Color32(255, 0, 0, 255), new Color32(255, 71, 0, 255), new Color32(255, 218, 0, 255), new Color32(114, 255, 0, 255), new Color32(0, 255, 255, 255), new Color32(0, 74, 255, 255), new Color32(104, 0, 255, 255), new Color32(255, 35, 255, 255)};
    private int[] barr = new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
    private int[] stage = new int[2];
    private int[] flashes = new int[7];
    private bool unlock;
    private bool reset;
    private RaycastHit[] allHit;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
            StartCoroutine(Strike());
    }

    private void Update()
    {
        if (!TwitchPlaysActive)
            allHit = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
        else
            allHit = Physics.RaycastAll(new Ray(tpCursor.transform.position, -transform.up));
        bool found = false;
        bool found2 = false;
        bool tpHitWall = false;
        for (int i = 0; i < allHit.Length; i++)
        {
            if (allHit[i].collider.name == colliders[0].name)
                found = true;
            if (TwitchPlaysActive)
            {
                for (int j = 1; j < 9; j++)
                {
                    if (colliders[j].name == allHit[i].collider.name)
                    {
                        tpHitWall = true;
                        break;
                    }
                }
                for (int j = 9; j < colliders.Length; j++)
                {
                    if (colliders[j].name == allHit[i].collider.name)
                    {
                        tpButton = j - 9;
                        found2 = true;
                        break;
                    }
                }
            }
        }
        if (mazereveal[8].enabled && !reset && (!found || tpHitWall))
            StartCoroutine(Strike());
        if (TwitchPlaysActive)
        {
            tpCursor.transform.localPosition = tpAnchor.localPosition;
            if (!tpTestCursor.activeSelf)
                tpTestAnchor.localPosition = tpAnchor.localPosition;
            tpTestCursor.transform.localPosition = tpTestAnchor.localPosition;
            if (!found)
                tpOffField = true;
            else
                tpOffField = false;
            if (!found2)
                tpButton = -1;
        }
    }

    private void Start()
    {
        moduleID = ++moduleIDCounter;
        cb = cbmode.ColorblindModeActive;
        foreach (Collider c in colliders)
            c.name = "ssn" + c.name + moduleID;
        barr = barr.Shuffle();
        foreach(KMSelectable m in mazewalls)
        {
            m.OnHighlight = delegate ()
            {
                if(!reset)
                  StartCoroutine(Strike());
            };
        }
        modSelect.OnDefocus += delegate () { if (!TwitchPlaysActive) StartCoroutine(Strike()); };
        mazereveal[8].enabled = false;
        foreach(Light l in lights)
        {
            l.range *= module.transform.lossyScale.x;
        }
        for(int i = 0; i < 8; i++)
        {
            mazereveal[i].enabled = false;
            mazes[i].SetActive(false);
            bulbs[i].material = bulbcols[barr[i]];
            lights[i].color = cols[barr[i]];
        }
        Setup();
        foreach (KMSelectable button in buttons)
        {
            int b = buttons.IndexOf(button);
            button.OnInteract = delegate ()
            {
                if (!reset)
                {
                    if (!moduleSolved)
                    {
                        if (b == flashes[stage[1]])
                        {
                            unlock = true;
                            if (stage[1] < (2 * stage[0]) + 2)
                            {
                                StartCoroutine(Flash(b));
                                if (stage[1] < 1)
                                {
                                    StopCoroutine("Seq");
                                    mazereveal[8].enabled = true;
                                }
                                else
                                    mazes[barr[flashes[stage[1] - 1]]].SetActive(false);
                                stage[1]++;
                                mazes[barr[flashes[stage[1]]]].SetActive(true);
                                stage[1]++;
                            }
                            else
                            {
                                mazes[barr[flashes[stage[1] - 1]]].SetActive(false);
                                stage[1] = 0;
                                lights[stage[0] + 8].enabled = true;
                                mazereveal[8].enabled = false;
                                if (stage[0] > 1)
                                {
                                    button.AddInteractionPunch(-1);
                                    unlock = false;
                                    Audio.PlaySoundAtTransform("FlashFinal", transform);
                                    for (int i = 0; i < 8; i++)
                                        StartCoroutine(Flash(i));
                                    module.HandlePass();
                                    moduleSolved = true;
                                }
                                else
                                {
                                    StartCoroutine(Flash(b));
                                    button.AddInteractionPunch(-0.5f);
                                    stage[0]++;
                                    Setup();
                                }
                            }
                        }
                    }
                    else
                    {
                        StartCoroutine(Flash(b));
                    }
                }
                return false;
            };
        }
        module.OnActivate += TPCheck;
    }

    private void TPCheck()
    {
        if (TwitchPlaysActive)
        {
            tpCursor.SetActive(true);
            tpSpeed = Random.Range(0.025f, 0.035f);
        }
    }

    private void Setup()
    {
        string[] card = new string[8] { "NNW", "NNE", "ENE", "ESE", "SSE", "SSW", "WSW", "WNW"};
        for(int i = 0; i < (2 * stage[0]) + 3; i++)
        {
            flashes[i] = Random.Range(0, 8);
            while (i > 1 && i % 2 == 0 && flashes[i] == flashes[i - 2])
                flashes[i] = Random.Range(0, 8);
        }
        List<string> map = new List<string> { };
        for(int i = 0; i < (2 * stage[0]) + 3; i++)
        {
            if (i % 2 == 0)
                map.Add(card[flashes[i]]);
            else
                map.Add("-" + "ROYGCBBP"[barr[flashes[i]]] + ">");
        }
        Debug.LogFormat("[Simon Senses #{0}] Stage {1}: {2}", moduleID, stage[0] + 1, map.Join());
        StartCoroutine("Seq");
    }

    private IEnumerator Seq()
    {
        yield return new WaitForSeconds(1);
        while(stage[1] < 1)
        {
            for(int i = 0; i < (2 * stage[0]) + 3; i++)
            {
                StartCoroutine(Flash(flashes[i]));
                yield return new WaitForSeconds(0.8f);
            }
            yield return new WaitForSeconds(1);
        }
    }

    private IEnumerator Flash(int b)
    {
        if(moduleSolved || unlock)
            Audio.PlaySoundAtTransform("Flash" + (barr[b] + 1), bulbs[b].transform);
        lights[b].enabled = true;
        filaments[b].material = io[0];
        if(stage[1] < 1 && cb)
            cbtext.text = "ROYGCBVP"[barr[b]].ToString();
        yield return new WaitForSeconds(0.5f);
        lights[b].enabled = false;
        filaments[b].material = io[1];
        cbtext.text = "";
    }

    private IEnumerator Strike()
    {
        if (stage[1] > 0 && !reset)
        {
            reset = true;
            mazereveal[barr[flashes[stage[1] - 1]]].enabled = true;
            module.HandleStrike();
            yield return new WaitForSeconds(2.5f);
            mazereveal[8].enabled = false;
            for (int i = 0; i < 8; i++)
            {
                mazereveal[i].enabled = false;
                mazes[i].SetActive(false);
            }
            stage[1] = 0;
            StartCoroutine("Seq");
            reset = false;
        }
    }

    //twitch plays
    public GameObject tpCursor;
    public Transform tpAnchor;
    public GameObject tpTestCursor;
    public Transform tpTestAnchor;
    private bool TwitchPlaysActive;
    private bool tpOffField;
    private float tpTime;
    private float tpSpeed;
    private int tpButton = -1;
    private bool TwitchShouldCancelCommand;
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} time <seconds> [Sets the amount of time the cursor moves in seconds] | !{0} angle <degrees> [Sets the movement direction of the cursor in degrees starting at 0 from north] | !{0} test [Test to see where the cursor will go with the current settings] | !{0} move [Moves the cursor with the current settings] | !{0} press [Presses the button the cursor is currently over] | !{0} colorblind [Toggles colorblind mode] | On Twitch Plays this module uses a fake cursor that moves at a random fixed speed and strikes will not occur for deselecting the module";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*colorblind|cb\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            cb = !cb;
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (reset)
                yield return "sendtochaterror You cannot press a button while the module is resetting!";
            else if (tpButton == -1)
                yield return "sendtochaterror The cursor is not currently over a button!";
            else
            {
                yield return null;
                buttons[tpButton].OnInteract();
            }
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*move\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (reset)
                yield return "sendtochaterror You cannot move the cursor while the module is resetting!";
            else
            {
                yield return null;
                float t = 0f;
                while (t < tpTime)
                {
                    yield return null;
                    t += Time.deltaTime;
                    if (tpOffField)
                    {
                        tpAnchor.localPosition = new Vector3(0, 0.0183f, 0);
                        yield break;
                    }
                    tpAnchor.Translate(Vector3.forward * Time.deltaTime * tpSpeed);
                    if (reset)
                    {
                        yield return "strike";
                        yield break;
                    }
                }
            }
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*test\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            tpTestCursor.SetActive(true);
            float t = 0f;
            while (t < tpTime)
            {
                yield return null;
                t += Time.deltaTime;
                if (TwitchShouldCancelCommand)
                    break;
                tpTestAnchor.Translate(Vector3.forward * Time.deltaTime * tpSpeed);
            }
            tpTestCursor.SetActive(false);
            tpTestAnchor.localPosition = tpAnchor.localPosition;
            if (TwitchShouldCancelCommand)
                yield return "cancelled";
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*time\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a time in seconds!";
            else if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else
            {
                float time = -1;
                if (!float.TryParse(parameters[1], out time))
                {
                    yield return "sendtochaterror!f The specified time '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                yield return null;
                tpTime = time;
            }
            yield break;
        }
        if (Regex.IsMatch(parameters[0], @"^\s*angle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify the angle in degrees!";
            else if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else
            {
                int angle = -1;
                if (!int.TryParse(parameters[1], out angle))
                {
                    yield return "sendtochaterror!f The specified angle '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                yield return null;
                Vector3 newRot = new Vector3(0, angle, 0);
                tpAnchor.transform.localEulerAngles = newRot;
                tpTestAnchor.transform.localEulerAngles = newRot;
            }
            yield break;
        }
    }
}