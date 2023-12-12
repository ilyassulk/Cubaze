using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetupSpavn : MonoBehaviour
{
    private enum PhaseOfApp
    {
        INIT = 0,
        GEN = 1,
        GAME = 2,
        FINISH = 3,
        STOP = 4
    }

    private enum ColorsPanel
    {
        Blue = 1,
        Red = 2,
        Black = 3
    }

    public GameObject CubeOrig; //Материал стенок
    private int MazeSize = 1;//Размер лабиринтов
    public int MazesCount;//Количество лабиринтов
    public float borderSize;//Размер стен
    public float playerSize;//Размер игрока
    public GameObject playerMat; //Материал игрока
    public GameObject FloorMat; //Материал пола
    public GameObject Camera; //Камера
    public UnityEngine.UI.Button ButtonStart;
    public UnityEngine.UI.Button ButtonUp;
    public UnityEngine.UI.Button ButtonDown;
    public UnityEngine.UI.Text TextCountMazes;
    public Canvas Canv;
    public GameObject PanelBig;
    public GameObject PanelSmall;
    public float speedToGradientSmallPanel;
    public UnityEngine.UI.Button ButtonExit;
    public UnityEngine.UI.Text TextButtonExit;
    public GameObject PanelTimer;
    public UnityEngine.UI.Text TextTimeBetter;
    public UnityEngine.UI.Text TextTimeNow;
    public UnityEngine.UI.Text TextDifNow;
    public UnityEngine.UI.Text TextLevelNow;

    private GameObject Floor; //Объект пола
    private List<List<GameObject>> Cubes; //Скелет лабиринта
    private List<Maze> Mazes; //Лаиринты для прохождения
    private GameObject player; //Объект игрока
    private Vector3 distanceFromObject; //Позиция камеры относительно игрока
    public int speedRoll = 300; //Скорость поворота купа игрока
    public float speedMute = 1; //Скорость затемнения
    private bool isMoving = false; //Находится ли игрок сейчас в движении
    private bool isMuting = false; //Анимация заднего фона
    private ColorsPanel colorNow = ColorsPanel.Blue;
    private List<int> results = new List<int>();
    private int difficultCur = 0;

    private PhaseOfApp CurPhaseOfApp = 0;
    private int curLevelBuild = -1;
    private int curLevelNow = 0;

    private float timer = 0;

    private String curLevelStr = "Лабиринт: ";
    private String curDifStr = "Сложность: ";
    private String curTimeStr = "Текущий:";
    private String bestTimeStr = "Лучший:";


    // Start is called before the first frame update
    void Start()
    {
        for(int dif = 1; dif <= 99; dif++)
        {
            int secondsDif = PlayerPrefs.GetInt(dif.ToString());
            results.Add(secondsDif);
        }

        distanceFromObject = new Vector3(0, 10, -6);
        Cubes = new List<List<GameObject>>();
        Camera.transform.position = Vector3.zero;
        Camera.transform.rotation = Quaternion.identity;

        player = Instantiate(playerMat, new Vector3(1, playerSize, 1), playerMat.transform.rotation);
        LookAt(player.transform.position);

        ButtonStart.GetComponent<Button>().onClick.AddListener(TaskOnClickButtonStart);
        ButtonUp.GetComponent<Button>().onClick.AddListener(TaskOnClickButtonUp);
        ButtonDown.GetComponent<Button>().onClick.AddListener(TaskOnClickButtonDown);
        ButtonExit.GetComponent<Button>().onClick.AddListener(TaskOnClickButtonExit);

        PanelSmall.GetComponent<Image>().color = Color.blue;

        StartCoroutine(gradientSmallPanel());

        CurPhaseOfApp = PhaseOfApp.STOP;
        setCurrentLevel("-", "-");


    }

    IEnumerator gradientSmallPanel()
    {
        while (true)
        {
            if (PanelSmall.GetComponent<Image>().color.b > 0.9)
                colorNow = ColorsPanel.Blue;

            if (PanelSmall.GetComponent<Image>().color.r > 0.9)
                colorNow = ColorsPanel.Red;

            if (PanelSmall.GetComponent<Image>().color.r < 0.1 && PanelSmall.GetComponent<Image>().color.g < 0.1 && PanelSmall.GetComponent<Image>().color.b < 0.1)
                colorNow = ColorsPanel.Black;

            if (colorNow == ColorsPanel.Blue)
            {
                PanelSmall.GetComponent<Image>().color = Color.Lerp(PanelSmall.GetComponent<Image>().color, Color.red, Time.deltaTime * speedToGradientSmallPanel);
                ButtonExit.GetComponent<Image>().color = Color.Lerp(PanelSmall.GetComponent<Image>().color, Color.red, Time.deltaTime * speedToGradientSmallPanel);
            }

            if (colorNow == ColorsPanel.Red)
            {
                PanelSmall.GetComponent<Image>().color = Color.Lerp(PanelSmall.GetComponent<Image>().color, Color.black, Time.deltaTime * speedToGradientSmallPanel);
                ButtonExit.GetComponent<Image>().color = Color.Lerp(PanelSmall.GetComponent<Image>().color, Color.black, Time.deltaTime * speedToGradientSmallPanel);
            }

            if (colorNow == ColorsPanel.Black)
            {
                PanelSmall.GetComponent<Image>().color = Color.Lerp(PanelSmall.GetComponent<Image>().color, Color.blue, Time.deltaTime * speedToGradientSmallPanel);
                ButtonExit.GetComponent<Image>().color = Color.Lerp(PanelSmall.GetComponent<Image>().color, Color.blue, Time.deltaTime * speedToGradientSmallPanel);
            }

            PanelSmall.GetComponent<Image>().color = new Color(PanelSmall.GetComponent<Image>().color.r, PanelSmall.GetComponent<Image>().color.g, PanelSmall.GetComponent<Image>().color.b, 1);

            yield return null;
        }
    }

    // Update is called once per frame
    void TaskOnClickButtonStart()
    {
        player.SetActive(false);
        CurPhaseOfApp = PhaseOfApp.GEN;
        
        curLevelBuild = -1;
        curLevelNow = 0;
    }

    void TaskOnClickButtonExit()
    {
        if (CurPhaseOfApp == PhaseOfApp.GAME)
        {
            CurPhaseOfApp = PhaseOfApp.FINISH;
        }
        else
        {
            for (int dif = 1; dif <= 99; dif++)
            {
                PlayerPrefs.SetInt(dif.ToString(), results[dif-1]);
            }
            Application.Quit();
        }
    }

    void TaskOnClickButtonUp()
    {
        MazeSize++;
    }

    void TaskOnClickButtonDown()
    {
        MazeSize--;
    }

    void Update()
    {
        if (isMuting) return;

        setCurrentResult();
        setCurrentDif();

        if (CurPhaseOfApp == PhaseOfApp.INIT)
        {
            TextCountMazes.text = "Сложность: "+MazeSize.ToString();
            TextButtonExit.text = "СОХРАНИТЬ И ВЫЙТИ";
            MazeSize = Math.Max(1, MazeSize);
            MazeSize = Math.Min(99, MazeSize);
            difficultCur = MazeSize;
            setBetterResult();
        }

        if (CurPhaseOfApp == PhaseOfApp.GEN)
        {
            MazeSize += 4;
            MazesCount = 5;
            //if (MazeSize < 10)
            //    MazesCount = 5;
            //else if (MazeSize < 16)
            //    MazesCount = 3;
            //else
            //    MazesCount = 1;

            MazesCount = Math.Max(1, MazesCount);
            MazeSize = Math.Max(5, MazeSize);

            Mazes = new List<Maze>();
            for(int i = 0; i < MazesCount; i++)
            {
                Mazes.Add(new Maze());
                Mazes[i] = createMaze(MazeSize*2+1);
            }

            Mazes.Sort(delegate(Maze a, Maze b) { 
            if(a.pointsEasy > b.pointsEasy)
                    return 1;
            else
                    return -1;
            });

            CurPhaseOfApp = PhaseOfApp.GAME;
            disableUI();

            int MazeSizeForGen = MazeSize * 2 + 1;
            Cubes.Clear();
            for (int i = 0; i < MazeSizeForGen; i++)
            {
                Cubes.Add(new List<GameObject>());
                for (int j = 0; j < MazeSizeForGen; j++)
                {
                    Cubes[i].Add(Instantiate(CubeOrig, new Vector3(i, borderSize, j), CubeOrig.transform.rotation));
                    Cubes[i][j].transform.localScale = new Vector3(borderSize, borderSize, borderSize);
                    Cubes[i][j].GetComponent<MeshRenderer>().material.color = Color.black;
                    Cubes[i][j].SetActive(false);
                }
            }

            timer = 0;
            StartCoroutine(timerRun());
        }

        if (CurPhaseOfApp == PhaseOfApp.GAME)
        {
            TextButtonExit.text = "ЗАКОНЧИТЬ ПОПЫТКУ";
            setCurrentLevel((curLevelNow+1).ToString(),MazesCount.ToString());

            if (curLevelBuild < curLevelNow)
            {
                if (curLevelNow >= MazesCount)
                {
                    haveNewResult((int)timer, difficultCur);
                    CurPhaseOfApp = PhaseOfApp.FINISH;
                    goto FinishPhase;
                }

                StartCoroutine(biUping());
                if (isMuting) return;
                if (curLevelBuild >= 0)
                    DestroyLevel();
                BuildLevel();
                StartCoroutine(biDowning());

                curLevelBuild++;
            }

            //scroll();

            if (isMoving)
            {
                return;
            }

            Move();

            if (player.transform.position.x == Mazes[curLevelBuild].finishX && player.transform.position.z == Mazes[curLevelBuild].finishY)
                curLevelNow++;
        }

        FinishPhase:

        if(CurPhaseOfApp == PhaseOfApp.FINISH)
        {
            setCurrentLevel("-", "-");
            StartCoroutine(biUping());
            if (isMuting) return;
            DestroyLevel();
            CurPhaseOfApp = PhaseOfApp.STOP;
        }

        if(CurPhaseOfApp == PhaseOfApp.STOP)
        {
            MazeSize = difficultCur;
            enableUI();
            Destroy(Floor);
            for(int i = 0; i < Cubes.Count; i++)
            {
                for(int j = 0; j < Cubes[i].Count;j++)
                    Destroy(Cubes[i][j]);
            }
            CurPhaseOfApp = PhaseOfApp.INIT;
        }
    }

    void setBetterResult()
    {
        if(results[difficultCur - 1] <= 1)
        {
            TextTimeBetter.text = bestTimeStr + " --" + ":" + "--";
            return;
        }
        int minutes = results[difficultCur - 1] / 60;
        int seconds = results[difficultCur - 1] - minutes*60;
        TextTimeBetter.text = bestTimeStr + " " + getDoubleCellsNum(minutes) +":"+getDoubleCellsNum(seconds);
    }

    void setCurrentResult()
    {
        if (CurPhaseOfApp != PhaseOfApp.GAME)
        {
            TextTimeNow.text = curTimeStr + " --" + ":" + "--";
            return;
        }
        int timerInt = ((int)timer);
        int minutes = timerInt / 60;
        int seconds = timerInt - minutes * 60;
        TextTimeNow.text = curTimeStr + " " + getDoubleCellsNum(minutes) + ":" + getDoubleCellsNum(seconds);
    }

    String getDoubleCellsNum(int a)
    {
        String res = a.ToString();
        if(res.Length == 1)
        {
            res = "0" + res;
        }
        return res;
    }

    void setCurrentDif()
    {
        TextDifNow.text = curDifStr + difficultCur.ToString();
    }

    void setCurrentLevel(string now, string all)
    {
        TextLevelNow.text = curLevelStr+now+"/"+all;
    }

    void haveNewResult(int result, int dif)
    {
        if (results[dif - 1] <= 1 || results[dif - 1] > result)
        {
            results[dif - 1] = result;
        }
    }

    //void scroll()
    //{
    //    float mw = Input.GetAxis("Mouse ScrollWheel");
    //    if (mw > 0.1)
    //    {
    //        distanceFromObject += new Vector3(0, 1, (float)-0.5);/*Приближение*/

    //    }
    //    if (mw < -0.1)
    //    {
    //        distanceFromObject += new Vector3(0, -1, (float)0.5);/*Отдаление*/

    //    }

    //    distanceFromObject.z = Math.Min(distanceFromObject.z, -5);
    //    distanceFromObject.z = Math.Max(distanceFromObject.z, -10);

    //    distanceFromObject.y = Math.Max(distanceFromObject.y, 5);
    //    distanceFromObject.y = Math.Min(distanceFromObject.y, 50);

    //    Vector3 positionToGo = player.transform.position + distanceFromObject;
    //    Vector3 smoothPosition = Vector3.Lerp(a: Camera.transform.position, b: positionToGo, t: Time.deltaTime * 2);
    //    Camera.transform.position = smoothPosition;
    //    Camera.transform.LookAt(player.transform.position);
    //}

    void disableUI()
    {
        ButtonStart.transform.localScale = Vector3.zero;
        ButtonUp.transform.localScale = Vector3.zero;
        ButtonDown.transform.localScale = Vector3.zero;
        TextCountMazes.transform.localScale = Vector3.zero;
        PanelSmall.transform.localScale = Vector3.zero;
    }

    void enableUI()
    {
        ButtonStart.transform.localScale = Vector3.one;
        ButtonUp.transform.localScale = Vector3.one;
        ButtonDown.transform.localScale = Vector3.one;
        TextCountMazes.transform.localScale = Vector3.one * 3;
        PanelSmall.transform.localScale = Vector3.one;
    }

    void BuildLevel()
    {
        int MazeSizeForGen = MazeSize * 2 + 1;
        
        Floor = Instantiate(FloorMat, new Vector3(MazeSizeForGen * (float)0.5, 0, MazeSizeForGen * (float)0.5), FloorMat.transform.rotation);
        Floor.transform.localScale = new Vector3(MazeSizeForGen +70, 1, MazeSizeForGen +70);
        Floor.SetActive(true);

        buildMaze(Mazes[curLevelNow]);

        player = Instantiate(playerMat, new Vector3(Mazes[curLevelNow].startX, playerSize, Mazes[curLevelNow].startY), playerMat.transform.rotation);
        player.GetComponent<MeshRenderer>().material.color = Color.blue;
        player.SetActive(true);

        Cubes[Mazes[curLevelNow].finishX][Mazes[curLevelNow].finishY].GetComponent<MeshRenderer>().material.color = Color.red;
        Cubes[Mazes[curLevelNow].finishX][Mazes[curLevelNow].finishY].SetActive(true);

        Camera.transform.position = player.transform.position + distanceFromObject;
        LookAt(player.transform.position);
    }

    void DestroyLevel()
    {
        Floor.SetActive(false);

        destroyMaze();

        player.SetActive(false);
    }

    void destroyMaze()
    {
        for(int i = 0; i < Cubes.Count; i++)
        {
            for (int j = 0; j < Cubes[i].Count; j++)
            {
                Cubes[i][j].SetActive(false);
                Cubes[i][j].GetComponent<MeshRenderer>().material.color = Color.black;
            }
        }
    }

    IEnumerator timerRun()
    {
        while (CurPhaseOfApp == PhaseOfApp.GAME)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator biDowning()
    {
        isMuting = true;

        while (PanelBig.GetComponent<Image>().color.a > 0.1)
        {
            PanelBig.GetComponent<Image>().color = Color.Lerp(PanelBig.GetComponent<Image>().color, new Color(PanelBig.GetComponent<Image>().color.r, PanelBig.GetComponent<Image>().color.g, PanelBig.GetComponent<Image>().color.b, 0), Time.deltaTime * speedMute);
            yield return null;
        }
        PanelBig.GetComponent<Image>().color = new Color(PanelBig.GetComponent<Image>().color.r, PanelBig.GetComponent<Image>().color.g, PanelBig.GetComponent<Image>().color.b, 0);

        isMuting = false;
    }
    IEnumerator biUping()
    {
        isMuting = true;

        while (PanelBig.GetComponent<Image>().color.a < 0.9)
        {
            PanelBig.GetComponent<Image>().color = Color.Lerp(PanelBig.GetComponent<Image>().color, new Color(PanelBig.GetComponent<Image>().color.r, PanelBig.GetComponent<Image>().color.g, PanelBig.GetComponent<Image>().color.b, 1), Time.deltaTime * speedMute);
            yield return null;
        }
        PanelBig.GetComponent<Image>().color = new Color(PanelBig.GetComponent<Image>().color.r, PanelBig.GetComponent<Image>().color.g, PanelBig.GetComponent<Image>().color.b, 1);

        isMuting = false;
    }


    void Move()
    {
        if ((Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) && !Mazes[curLevelBuild].borders[(int)player.transform.localPosition.x][(int)player.transform.localPosition.z + 1])
        {
            StartCoroutine(Roll(Vector3.forward));
        }
        else if ((Input.GetKey(KeyCode.DownArrow)  || Input.GetKey(KeyCode.S)) && !Mazes[curLevelBuild].borders[(int)player.transform.localPosition.x][(int)player.transform.localPosition.z - 1])
        {
            StartCoroutine(Roll(Vector3.back));
        }
        else if ((Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) && !Mazes[curLevelBuild].borders[(int)player.transform.localPosition.x - 1][(int)player.transform.localPosition.z])
        {
            StartCoroutine(Roll(Vector3.left));
        }
        else if ((Input.GetKey(KeyCode.RightArrow)  || Input.GetKey(KeyCode.D)) && !Mazes[curLevelBuild].borders[(int)player.transform.localPosition.x + 1][(int)player.transform.localPosition.z])
        {
            StartCoroutine(Roll(Vector3.right));
        }
    }
    IEnumerator Roll(Vector3 direction)
    {
        isMoving = true;

        float remAngle = 90;
        Vector3 rotCenter = player.transform.position + direction / 2 + Vector3.down / 2;
        Vector3 rotAxis = Vector3.Cross(Vector3.up, direction);

        while(remAngle > 0)
        {
            float rotAngle = Math.Min(Time.deltaTime * speedRoll, remAngle);
            player.transform.RotateAround(rotCenter, rotAxis, rotAngle);
            remAngle -= rotAngle;
            LookAt(player.transform.position);
            yield return null;
        }

        isMoving = false;
        player.transform.position = new Vector3((float)Math.Round(player.transform.localPosition.x), (float)Math.Round(player.transform.localPosition.y), (float)Math.Round(player.transform.localPosition.z));
        
    }
    void LookAt(Vector3 newPos)
    {
        Vector3 positionToGo = newPos + distanceFromObject;
        positionToGo.y = distanceFromObject.y;
        Vector3 smoothPosition = Vector3.Lerp(a: Camera.transform.position, b: positionToGo, t: Time.deltaTime * 2);
        Camera.transform.position = smoothPosition;

        Vector3 positionToLook = newPos;
        positionToLook.y = 0;
        Camera.transform.LookAt(positionToLook);
    }

    void buildMaze(Maze maze) { 
        for(int i = 0; i < maze.borders.Count; i++)
        {
            for(int j = 0; j < maze.borders[i].Count; j++)
            {
                Cubes[i][j].SetActive(maze.borders[i][j]);
            }
        }
    }

    int getSteps(Cell st, Cell fn, List<List<bool>> borders)
    {
        int stepsMin = int.MaxValue;
        List<List<bool>> used = new List<List<bool>>();
        for (int i = 0; i < borders.Count; i++)
        {
            used.Add(new List<bool>());
            for (int j = 0; j < borders[i].Count; j++)
                used[i].Add(false);
        }

        Stack<StepBFS> cells = new Stack<StepBFS>();
        cells.Push(new StepBFS(st, 0));
        while (cells.Count > 0)
        {
            StepBFS step = cells.Pop();
            Cell curCell = step.cell;

            if (curCell.x == fn.x && curCell.y == fn.y)
            {
                stepsMin = Math.Min(stepsMin, step.steps);
                break;
            }

            List<Cell> neighbors = getNeighbors(curCell, used);
            for (int i = 0; i < neighbors.Count; i++)
            {
                double absX = neighbors[i].x - curCell.x;
                double absY = neighbors[i].y - curCell.y;
                absX = Math.Round(absX / 2);
                absY = Math.Round(absY / 2);
                if (borders[curCell.x + (int)absX][curCell.y + (int)absY])
                    continue;
                used[neighbors[i].x][neighbors[i].y] = true;
                cells.Push(new StepBFS(neighbors[i], step.steps + 1));
            }
        }

        return stepsMin;
    }

    List<Cell> getNeighbors(Cell cell, List<List<bool>> used)
    {
        List<Cell> neighbors = new List<Cell>();

        if(cell.x >= 3 && !used[cell.x - 2][cell.y])
            neighbors.Add(new Cell(cell.x-2, cell.y));

        if (cell.y >= 3 && !used[cell.x][cell.y - 2])
            neighbors.Add(new Cell(cell.x, cell.y - 2));

        if (cell.x <= used.Count- 4 && !used[cell.x + 2][cell.y])
            neighbors.Add(new Cell(cell.x+2, cell.y));

        if (cell.y <= used.Count - 4 && !used[cell.x][cell.y + 2])
            neighbors.Add(new Cell(cell.x, cell.y+2));

        return neighbors;
    }

    Maze createMaze(int size)
    {
        int pointsEasy = 0;
        List<List<bool>> borders = new List<List<bool>>();
        List<List<bool>> used = new List<List<bool>>();
        for (int i = 0; i < size; i++)
        {
            borders.Add(new List<bool>());
            used.Add(new List<bool>());
            for (int j = 0; j < size; j++)
            {
                if (i % 2 == 1 && j % 2 == 1)
                {
                    borders[i].Add(false);
                }
                else
                {
                    borders[i].Add(true);
                }
                used[i].Add(false);
            }

        }

        Stack<Cell> cells = new Stack<Cell>();
        cells.Push(new Cell(size - 2, size - 2));
        used[size - 2][size - 2] = true;
        while (cells.Count > 0)
        {
            Cell curCell = cells.Pop();
            List<Cell> neighbors = getNeighbors(curCell, used);
            while (neighbors.Count > 0)
            {

                int iNextCell = UnityEngine.Random.Range(0, neighbors.Count);
                Cell nextCell = neighbors[iNextCell];

                if (nextCell.x > curCell.x)
                    borders[nextCell.x - 1][nextCell.y] = false;
                else if (nextCell.x < curCell.x)
                    borders[nextCell.x + 1][nextCell.y] = false;
                else if (nextCell.y > curCell.y)
                    borders[nextCell.x][nextCell.y - 1] = false;
                else if (nextCell.y < curCell.y)
                    borders[nextCell.x][nextCell.y + 1] = false;

                used[nextCell.x][nextCell.y] = true;

                neighbors.RemoveAt(iNextCell);
                cells.Push(nextCell);
            }
        }

        Cell start = new Cell(1, 1);
        Cell finish = new Cell(MazeSize * 2 + 1 - 2, MazeSize * 2 + 1 - 2);

        pointsEasy = getSteps(start, finish, borders);

        return new Maze(pointsEasy, borders,start.x, start.y, finish.x, finish.y);
    }
}