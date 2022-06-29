using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private bool cb;
    private readonly Color32[] cols = new Color32[8] { new Color32(255, 0, 0, 255), new Color32(255, 71, 0, 255), new Color32(255, 218, 0, 255), new Color32(114, 255, 0, 255), new Color32(0, 255, 255, 255), new Color32(0, 74, 255, 255), new Color32(104, 0, 255, 255), new Color32(255, 35, 255, 255)};
    private int[] barr = new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
    private int[] stage = new int[2];
    private int[] flashes = new int[7];
    private bool unlock;
    private bool reset;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
            StartCoroutine(Strike());
    }

    private void Start()
    {
        moduleID = ++moduleIDCounter;
        cb = cbmode.ColorblindModeActive;
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
}