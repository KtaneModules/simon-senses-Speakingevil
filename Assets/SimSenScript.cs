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
        modSelect.OnDefocus += delegate () { StartCoroutine(Strike()); };
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
    private float tpSpeed;
    private int tpButton = -1;
    private bool TwitchShouldCancelCommand;
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <actions> [Makes the cursor perform the specified actions] | !{0} test <actions> [Tests the specified actions with a phantom cursor] | !{0} colorblind [Toggles colorblind mode] | On Twitch Plays this module uses a fake cursor that moves at a random fixed speed | Actions that can the cursor can do are a press or movement, presses are specified with 'press' while movements are a direction in degrees from north and a time in seconds separated by a space | Actions can be chained, for example: !{0} 45 2.5; -80 5; press";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = Regex.Replace(command, @"\s+", " ");
        if (Regex.IsMatch(command, @"^\s*colorblind|cb\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            cb = !cb;
            yield break;
        }
        bool isTest = false;
        if (command.ToLowerInvariant().StartsWith("test "))
        {
            isTest = true;
            command = command.Substring(5);
        }
        List<string> tpAngles = new List<string>();
        List<string> tpTimes = new List<string>();
        string[] parameters = command.Split(';');
        for (int i = 0; i < parameters.Length; i++)
        {
            parameters[i] = parameters[i].Trim();
            string[] args = parameters[i].Split(' ');
            if (args.Length > 2 || (args.Length == 1 && !args[0].EqualsIgnoreCase("press")))
            {
                yield return "sendtochaterror!f The specified action '" + parameters[i] + "' is invalid!";
                yield break;
            }
            if (args.Length == 2)
            {
                int ang = -1;
                if (!int.TryParse(args[0], out ang))
                {
                    yield return "sendtochaterror!f The specified action '" + parameters[i] + "' is invalid!";
                    yield break;
                }
                float time = -1f;
                if (!float.TryParse(args[1], out time))
                {
                    yield return "sendtochaterror!f The specified action '" + parameters[i] + "' is invalid!";
                    yield break;
                }
                tpAngles.Add(ang.ToString());
                tpTimes.Add(time.ToString());
            }
            else
            {
                tpAngles.Add("p");
                tpTimes.Add("p");
            }
        }
        yield return null;
        if (isTest)
            tpTestCursor.SetActive(true);
        for (int i = 0; i < parameters.Length; i++)
        {
            if (tpTimes[i] == "p")
            {
                if (isTest)
                {
                    int index = -1;
                    RaycastHit[] allHit = Physics.RaycastAll(new Ray(tpTestCursor.transform.position, -transform.up));
                    for (int k = 0; k < allHit.Length; k++)
                    {
                        for (int j = 9; j < colliders.Length; j++)
                        {
                            if (colliders[j].name == allHit[k].collider.name)
                            {
                                index = j;
                                break;
                            }
                        }
                    }
                    if (index != -1)
                        yield return "sendtochat The phantom cursor successfully \"pressed\" the " + colliders[index].name.Substring(9, 3) + " button on Module {1} (Simon Senses)!";
                    else
                        yield return "sendtochat The phantom cursor failed to \"press\" any button on Module {1} (Simon Senses)!";
                }
                else
                {
                    if (tpButton != -1)
                        buttons[tpButton].OnInteract();
                }
            }
            else
            {
                Vector3 newRot = new Vector3(0, int.Parse(tpAngles[i]), 0);
                tpAnchor.transform.localEulerAngles = newRot;
                tpTestAnchor.transform.localEulerAngles = newRot;
                float t = 0f;
                while (t < float.Parse(tpTimes[i]))
                {
                    yield return null;
                    t += Time.deltaTime;
                    if (isTest)
                    {
                        if (TwitchShouldCancelCommand)
                        {
                            i = parameters.Length;
                            break;
                        }
                        tpTestAnchor.Translate(Vector3.forward * Time.deltaTime * tpSpeed);
                    }
                    else
                    {
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
            }
        }
        if (isTest)
        {
            tpTestCursor.SetActive(false);
            tpTestAnchor.localPosition = tpAnchor.localPosition;
            if (TwitchShouldCancelCommand)
                yield return "cancelled";
        }
    }
}