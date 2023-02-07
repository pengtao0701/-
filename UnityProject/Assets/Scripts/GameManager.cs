using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public struct SaveLoadStruct
{
    // ��߷�
    public int BestScore;

    public float GameVolume;
}

public class GameManager : MonoBehaviour
{
    public SaveLoadStruct saveLoad;

    public AudioSource BgmAudio;
    private float DefaultVolume = 0.3f;

    private static GameManager gameManager;
    public static GameManager GM
    {
        get
        {
            return gameManager;
        }
        set
        {
            gameManager = value;
        }
    }

    public int xColumn;
    public int yRow;

    // ��������
    public GameObject gridPrefab;

    // ���ʱ�� 
    public float fillTime;

    // ��Ҫ�������������� 
    private GameFruit pressedFruit;
    private GameFruit enteredFruit;

    public enum FruitType
    {
        EMPTY, //��
        NORMAL, // ����ˮ��
        BARRIER, // �ϰ�
        SUPERSTAR,// ��������
        ROW_CLEAR, // ������
        COLUMN_CLEAR, // ������
        COUNT // ���
    }
    // �����ṹ�壬���л� ��FruitPrefabMap����ͼ����ʾ������struct��
    [System.Serializable]
    public struct FruitPrefab
    {
        public FruitType type;
        public GameObject prefab;
    }

    // ����FruitType ��ȡ��Ӧ��prefab
    public Dictionary<FruitType, GameObject> FruitPrefabMap;

    public FruitPrefab[] FruitPrefabs;
    // �ƶ�ˮ����Ч
    public AudioClip MoveFruitAudio;
    // ˮ������
    private GameFruit[,] Fruits;

    // ��ȡUI�ı�����
    public Text timeText;

    // һ����Ϸʱ��
    private float gameTime = 60;
    // �Ƿ���Ϸ����
    private bool gameOver = false;
    // �÷�
    public int playerScore;
    // �÷��ı�
    public Text playerScoreText;
    // ���ӷ���ʱ��
    private float AddScoreTime;
    // ��ǰ�÷�
    private float currentScore;
    // ����������յ÷�
    public Text FinalScoreText;
    // ���������ߵ÷�
    public Text FinalBestScoreText;
    // ��ü�¼����IMG
    public GameObject BestScoreText;
    // �������ֶ���
    public GameObject gameOverPanel;

    // ��ͣ���ֶ���
    public GameObject gameStopPanel;
    // ��ͣ
    private bool isStop = false;
    // ��ͣ/��ʼ��Ч
    public AudioClip gameStopAudio;

    // ǰ�ν���Я�̵ȴ�
    private float finishExchangeTime = 0.33f;
    private IEnumerator positonMoveCoroutine;

    private void Awake()
    {
        gameManager = this;
        if (PlayerPrefs.HasKey("GameVolume"))
        {
            BgmAudio.volume = PlayerPrefs.GetFloat("GameVolume");
        }
        else
        {
            BgmAudio.volume = DefaultVolume;
        }
        
    }

    // Start is called before the first frame update
    void Start()
    {
        // �ṹ��FruitPrefabMapʵ����
        FruitPrefabMap = new Dictionary<FruitType, GameObject>();
        for (int i = 0; i < FruitPrefabs.Length; i++)
        {
            if (!FruitPrefabMap.ContainsKey(FruitPrefabs[i].type))
            {
                FruitPrefabMap.Add(FruitPrefabs[i].type, FruitPrefabs[i].prefab);
            }
        }

        // position x=0.5 y=-1
        // scale x=1 y=1
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                // Quaternion.identity ����ת
                GameObject gridBox = Instantiate(gridPrefab, FixGridPosition(x, y), Quaternion.identity);
                gridBox.transform.SetParent(transform);
            }
        }

        // ���հ�
        Fruits = new GameFruit[xColumn, yRow];
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                CreateNewFruit(x, y, FruitType.EMPTY);
            }
        }
        int randomX = Random.Range(0, xColumn);
        int randomy = Random.Range(0, yRow);
        Destroy(Fruits[randomX, randomy].gameObject);
        CreateNewFruit(randomX, randomy, FruitType.BARRIER);

        // ���ˮ��
        // ����Я��IEnumeratorʱ��StartCoroutine
        StartCoroutine(AllFill());

    }

    // Update is called once per frame
    void Update()
    {
        if (gameOver)
        {
            // ����ʱ�ۼ�û��ɾ�ǿ����ʾ��ǰ����
            if (AddScoreTime != 0f)
            {
                playerScoreText.text = playerScore.ToString();
            }
            return;
        }
        // ʱ���ۼ�
        gameTime -= Time.deltaTime;

        //��Ϸ����
        if (gameTime <= 0)
        {
            bool isBestScore = false;

            gameTime = 0;
            // ������Ϸ����
            // ��ǰ����������߷� ����
            if (PlayerPrefs.HasKey("BestScore"))
            {
                saveLoad.BestScore = PlayerPrefs.GetInt("BestScore");

                if (playerScore > saveLoad.BestScore)
                {
                    PlayerPrefs.SetInt("BestScore", playerScore);
                    PlayerPrefs.Save();
                    isBestScore = true;
                }
            }
            else
            {
                // ��һ��������߷�
                PlayerPrefs.SetInt("BestScore", playerScore);
                PlayerPrefs.Save();
                isBestScore = true;
            }

            // ��ʾ�������
            // ���Ŷ���
            gameOverPanel.SetActive(true);
            if (isBestScore)
            {
                BestScoreText.SetActive(true);
            }

            FinalScoreText.text = playerScore.ToString();
            if (PlayerPrefs.HasKey("BestScore"))
            {
                FinalBestScoreText.text = PlayerPrefs.GetInt("BestScore").ToString();
            }
            else
            {
                FinalBestScoreText.text = "0";
            }
            // ������ʾ��óɼ�
            //PlayerPrefs.DeleteKey("BestScore");

            gameOver = true;
            return;
        }
        timeText.text = gameTime.ToString("0");

        if (AddScoreTime <= 0.05f)
        {
            AddScoreTime += Time.deltaTime;
        }
        else
        {
            if (currentScore < playerScore)
            {
                currentScore++;
                playerScoreText.text = currentScore.ToString();
                AddScoreTime = 0;
            }
        }
        
    }

    // ��������λ��
    public Vector3 FixGridPosition(int x, int y)
    {
        return new Vector3(transform.position.x - xColumn / 2f + x,
                                        transform.position.y + yRow / 2f - y,
                                        0);
    }

    // ����ˮ��
    public GameFruit CreateNewFruit(int x, int y, FruitType type)
    {
        GameObject newFruit = Instantiate(FruitPrefabMap[type], FixGridPosition(x, y), Quaternion.identity);
        newFruit.transform.parent = transform;

        Fruits[x, y] = newFruit.GetComponent<GameFruit>();
        Fruits[x, y].Init(x, y, this, type);

        return Fruits[x, y];
    }

    // ˮ�����ȫ������
    // IEnumerator Я��
    public IEnumerator AllFill()
    {
        // �Ƿ�������
        bool needRefill = true;

        while (needRefill)
        {
            yield return new WaitForSeconds(fillTime);

            while (Fill())
            {
                // �ȴ�fillTimeʱ�����ִ��
                yield return new WaitForSeconds(fillTime);
            }

            // ����Ѿ�ƥ��ŵ�ˮ��
            needRefill = ClearAllMatchedFruit();
        }
    }

    // �������
    public bool Fill()
    {
        // �ж�����Ƿ����
        bool isFilled = false;

        // ����������0�� -7��
        // ��Ⱦ�����Ǵ������ϣ����Դ������ϱ���,��һ�в��� �ڶ���-2��ʼ 
        // ������һ����0
        for (int y = yRow - 2; y >= 0; y--)
        {
            for (int x = 0; x < xColumn; x++)
            {
                GameFruit fruit = Fruits[x, y];

                if (fruit.canMove())
                {
                    GameFruit fruitBelow = Fruits[x, y + 1];
                    // ��ֱ���
                    // ����ǿ� ���Ϸ�ˮ�����²���
                    if (fruitBelow.Type == FruitType.EMPTY)
                    {
                        Destroy(fruitBelow.gameObject);
                        fruit.MovedComponent.Move(x, y + 1, fillTime);
                        // �Ϸ�ˮ��λ������һ��
                        Fruits[x, y + 1] = fruit;
                        // ��Ϊ�Ϸ�ˮ������ �������Ϸ����ɿգ��Դ�ѭ��
                        CreateNewFruit(x, y, FruitType.EMPTY);

                        isFilled = true;
                    }
                    else
                    {
                        // �����·����
                        // -1 ���� 1����
                        for (int down = -1; down <= 1; down++)
                        {
                            if (down != 0)
                            {
                                int downX = x + down;
                                // �ų��߽�λ��
                                if (downX >= 0 && downX < xColumn)
                                {
                                    // y + 1��һ�� downX ��߻��ұ�
                                    GameFruit downFruit = Fruits[downX, y + 1];
                                    if (downFruit.Type == FruitType.EMPTY)
                                    {
                                        // �ж��Ƿ�Ϊ��ֱ���
                                        bool canFill = true;
                                        for (int aboveY = y; aboveY >= 0; aboveY--)
                                        {
                                            // ��ֱ���������� break
                                            GameFruit fruitAbove = Fruits[downX, aboveY];
                                            if (fruitAbove.canMove())
                                            {
                                                break;
                                            }
                                            else if (!fruitAbove.canMove() &&
                                                fruitAbove.Type != FruitType.EMPTY)
                                            {
                                                canFill = false;
                                                break;
                                            }
                                        }

                                        // �޷���ֱ���
                                        // б�����
                                        if (!canFill)
                                        {
                                            Destroy(downFruit.gameObject);
                                            fruit.MovedComponent.Move(downX, y + 1, fillTime);
                                            Fruits[downX, y + 1] = fruit;
                                            CreateNewFruit(x, y, FruitType.EMPTY);
                                            isFilled = true;
                                            break;
                                        }
                                    }
                                }
                            }

                        }
                    }
                }

            }
        }

        // ��0���Ϸ�������
        for (int x = 0; x < xColumn; x++)
        {
            GameFruit fruit = Fruits[x, 0];
            if (FruitType.EMPTY == fruit.Type)
            {
                GameObject newFruit = Instantiate(FruitPrefabMap[FruitType.NORMAL], FixGridPosition(x, -1), Quaternion.identity);
                newFruit.transform.parent = transform;

                Fruits[x, 0] = newFruit.GetComponent<GameFruit>();
                Fruits[x, 0].Init(x, -1, this, FruitType.NORMAL);

                Fruits[x, 0].MovedComponent.Move(x, 0, fillTime);
                Fruits[x, 0].ColoredComponent.SetColor((ColorFrult.ColorType)Random.Range(0, Fruits[x, 0].ColoredComponent.NumColors));

                isFilled = true;
            }
        }

        return isFilled;
    }

    // �жϽ���ʱ�Ƿ�����
    private bool IsNeighbor(GameFruit fruit1, GameFruit fruit2)
    {
        // ��������ͬ �������������ֵ=1 ��֮
        return (fruit1.X == fruit2.X && Mathf.Abs(fruit1.Y - fruit2.Y) == 1) ||
                (fruit1.Y == fruit2.Y && Mathf.Abs(fruit1.X - fruit2.X) == 1);

    }

    // ˮ������
    private void ExchangeFruits(GameFruit fruit1, GameFruit fruit2)
    {
        if (fruit1.canMove() && fruit2.canMove())
        {
            Fruits[fruit1.X, fruit1.Y] = fruit2;
            Fruits[fruit2.X, fruit2.Y] = fruit1;

            if (MatchFruit(fruit1, fruit2.X, fruit2.Y) != null ||
                MatchFruit(fruit2, fruit1.X, fruit1.Y) != null ||
                fruit1.Type == FruitType.SUPERSTAR || fruit2.Type == FruitType.SUPERSTAR)
            {
                int tempX = fruit1.X;
                int tempY = fruit1.Y;

                fruit1.MovedComponent.Move(fruit2.X, fruit2.Y, fillTime);
                fruit2.MovedComponent.Move(tempX, tempY, fillTime);

                AudioSource.PlayClipAtPoint(MoveFruitAudio, Vector3.zero);

                if (fruit1.Type == FruitType.SUPERSTAR && fruit1.canClear() && fruit2.canClear())
                {
                    ClearColorFruit clearColor = fruit1.GetComponent<ClearColorFruit>();

                    if (clearColor != null)
                    {
                        clearColor.ClearColor = fruit2.ColoredComponent.Color;
                    }

                    ClearFruit(fruit1.X, fruit1.Y);
                }

                if (fruit2.Type == FruitType.SUPERSTAR && fruit1.canClear() && fruit2.canClear())
                {
                    ClearColorFruit clearColor = fruit2.GetComponent<ClearColorFruit>();

                    if (clearColor != null)
                    {
                        clearColor.ClearColor = fruit1.ColoredComponent.Color;
                    }

                    ClearFruit(fruit2.X, fruit2.Y);
                }
                // ���ˮ��
                ClearAllMatchedFruit();
                // ����λ
                StartCoroutine(AllFill());
            }
            else
            {
                int tempX = fruit1.X;
                int tempY = fruit1.Y;

                fruit1.MovedComponent.Move(fruit2.X, fruit2.Y, fillTime);
                fruit2.MovedComponent.Move(tempX, tempY, fillTime);

                AudioSource.PlayClipAtPoint(MoveFruitAudio, Vector3.zero);
                
                int tempMoved1X = fruit1.X;
                int tempMoved1Y = fruit1.Y;
                // ����Я�̵ȴ�ǰ�ν���
                positonMoveCoroutine = PositonMoveCoroutine(fruit1, fruit2, tempX, tempY, tempMoved1X, tempMoved1Y);
                StartCoroutine(positonMoveCoroutine);
                
            }

        }
    }

    // ����
    public void PressFruit(GameFruit fruit)
    {
        if (gameOver)
        {
            return;
        }
        pressedFruit = fruit;
    }
    // ����
    public void EnterFruit(GameFruit fruit)
    {
        if (gameOver)
        {
            return;
        }
        enteredFruit = fruit;
    }
    // �ɿ�
    public void ReleaseFruit()
    {
        if (gameOver)
        {
            return;
        }
        if (IsNeighbor(pressedFruit, enteredFruit))
        {
            ExchangeFruits(pressedFruit, enteredFruit);
        }
    }


    // ƥ�䷽��
    public List<GameFruit> MatchFruit(GameFruit fruit, int newX, int newY)
    {
        if (fruit.canColor())
        {
            ColorFrult.ColorType color = fruit.ColoredComponent.Color;
            List<GameFruit> matchRowFruit = new List<GameFruit>();
            List<GameFruit> matchLineFruit = new List<GameFruit>();
            List<GameFruit> finishedMatchFruit = new List<GameFruit>();

            // ----------------------��ƥ��--------------------------
            matchRowFruit.Add(fruit);

            // 0�� 1��
            for (int i = 0; i <= 1; i++)
            {
                for (int xDistance = 1; xDistance < xColumn; xDistance++)
                {
                    int x;
                    if (i == 0)
                    {
                        //����
                        x = newX - xDistance;
                    }
                    else
                    {
                        //����
                        x = newX + xDistance;
                    }
                    // �жϱ߽�
                    if (x < 0 || x >= xColumn)
                    {
                        break;
                    }
                    // ���������ˮ�� ������ͬһ��ˮ��,�ʹ���ƥ���б���
                    if (Fruits[x, newY].canColor() &&
                        Fruits[x, newY].ColoredComponent.Color == color)
                    {
                        matchRowFruit.Add(Fruits[x, newY]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchRowFruit.Count >= 3)
            {
                for (int i = 0; i < matchRowFruit.Count; i++)
                {
                    finishedMatchFruit.Add(matchRowFruit[i]);
                }
            }

            // LTƥ�� �е���ƥ��
            // �ж�ƥ���б����Ƿ����3��Ԫ��
            if (matchRowFruit.Count >= 3)
            {
                //Debug.Log("matchRowFruit.Count= " + matchRowFruit.Count);
                for (int i = 0; i < matchRowFruit.Count; i++)
                {
                    //Debug.Log("i= " + i);
                    // �����б�����ÿ��Ԫ�����б��� 
                    // 0 �� 1 ��
                    for (int j = 0; j <= 1; j++)
                    {
                        //Debug.Log("j= " + j);
                        for (int yDistance = 1; yDistance < yRow; yDistance++)
                        {
                            //Debug.Log("yDistance= " + yDistance);
                            int y;
                            if (j == 0)
                            {
                                y = newY - yDistance;
                            }
                            else
                            {
                                y = newY + yDistance;
                            }

                            if (y < 0 || y >= yRow)
                            {
                                break;
                            }
                            //Debug.Log("y= " + y);
                            //Debug.Log("matchRowFruit[i].X= " + matchRowFruit[i].X);
                            if (Fruits[matchRowFruit[i].X, y].canColor() &&
                                Fruits[matchRowFruit[i].X, y].ColoredComponent.Color == color)
                            {
                                matchLineFruit.Add(Fruits[matchRowFruit[i].X, y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchLineFruit.Count < 2)
                    {
                        matchLineFruit.Clear();
                    }
                    else
                    {
                        for (int k = 0; k < matchLineFruit.Count; k++)
                        {
                            finishedMatchFruit.Add(matchLineFruit[k]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchFruit.Count >= 3)
            {
                return finishedMatchFruit;
            }

            matchRowFruit.Clear();
            matchLineFruit.Clear();


            //------------------ ��ƥ��-----------------
            matchLineFruit.Add(fruit);

            // 0�� 1��
            for (int i = 0; i <= 1; i++)
            {
                for (int yDistance = 1; yDistance < yRow; yDistance++)
                {
                    int y;
                    if (i == 0)
                    {
                        //����
                        y = newY - yDistance;
                    }
                    else
                    {
                        //����
                        y = newY + yDistance;
                    }
                    // �жϱ߽�
                    if (y < 0 || y >= yRow)
                    {
                        break;
                    }
                    // ���������ˮ�� ������ͬһ��ˮ��,�ʹ���ƥ���б���
                    if (Fruits[newX, y].canColor() &&
                        Fruits[newX, y].ColoredComponent.Color == color)
                    {
                        matchLineFruit.Add(Fruits[newX, y]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchLineFruit.Count >= 3)
            {
                for (int i = 0; i < matchLineFruit.Count; i++)
                {
                    finishedMatchFruit.Add(matchLineFruit[i]);
                }
            }

            // LTƥ�� �е���ƥ��
            // �ж�ƥ���б����Ƿ����3��Ԫ��
            if (matchLineFruit.Count >= 3)
            {
                //Debug.Log("matchLineFruit.Count= " + matchLineFruit.Count);
                for (int i = 0; i < matchLineFruit.Count; i++)
                {
                    //Debug.Log("i= " + i);
                    // �����б�����ÿ��Ԫ�����б��� 
                    // 0 �� 1 ��
                    for (int j = 0; j <= 1; j++)
                    {
                        //Debug.Log("j= " + j);
                        for (int xDistance = 1; xDistance < xColumn; xDistance++)
                        {
                            //Debug.Log("xDistance= " + xDistance);
                            int x;
                            if (j == 0)
                            {
                                x = newX - xDistance;
                            }
                            else
                            {
                                x = newX + xDistance;
                            }

                            if (x < 0 || x >= xColumn)
                            {
                                break;
                            }
                            //Debug.Log("x= " + x);
                            //Debug.Log("matchLineFruit[i].Y= " + matchLineFruit[i].Y);
                            if (Fruits[x, matchLineFruit[i].Y].canColor() &&
                                Fruits[x, matchLineFruit[i].Y].ColoredComponent.Color == color)
                            {
                                matchRowFruit.Add(Fruits[x, matchLineFruit[i].Y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchRowFruit.Count < 2)
                    {
                        matchRowFruit.Clear();
                    }
                    else
                    {
                        for (int k = 0; k < matchRowFruit.Count; k++)
                        {
                            finishedMatchFruit.Add(matchRowFruit[k]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchFruit.Count >= 3)
            {
                return finishedMatchFruit;
            }
        }

        return null;
    }

    // ���ˮ��
    public bool ClearFruit(int x, int y)
    {
        if (Fruits[x, y].canClear() && !Fruits[x, y].ClearComponent.IsClearing)
        {
            Fruits[x, y].ClearComponent.Clear();
            CreateNewFruit(x, y, FruitType.EMPTY);
            // ����ϰ�
            ClearBarrier(x, y);
            return true;
        }

        return false;
    }

    // ����ϰ�
    private void ClearBarrier(int x, int y)
    {
        for (int aroundX = x - 1;  aroundX <= x + 1;  aroundX++)
        {
            // ��Ϊ�������ڸ��ӷ�Χ��
            if (aroundX != x && aroundX >= 0 && aroundX < xColumn)
            {
                // 
                //Debug.Log("FType =" + Fruits[aroundX, y].Type);
                //Debug.Log("canClear =" + Fruits[aroundX, y].canClear());
                if (Fruits[aroundX, y].Type == FruitType.BARRIER
                    && Fruits[aroundX, y].canClear())
                {
                    Fruits[aroundX, y].ClearComponent.Clear();
                    CreateNewFruit(aroundX, y, FruitType.EMPTY);
                }
            }
        }

        for (int aroundY = y - 1; aroundY <= y + 1; aroundY++)
        {
            // ��Ϊ�������ڸ��ӷ�Χ��
            if (aroundY != y && aroundY >= 0 && aroundY < yRow)
            {
                // 
                //Debug.Log("FType =" + Fruits[x, aroundY].Type);
                //Debug.Log("canClear =" + Fruits[x, aroundY].canClear());
                if (Fruits[x, aroundY].Type == FruitType.BARRIER
                    && Fruits[x, aroundY].canClear())
                {
                    Fruits[x, aroundY].ClearComponent.Clear();
                    CreateNewFruit(x, aroundY, FruitType.EMPTY);
                }
            }
        }
    }

    // ���ȫ��ƥ���б�ˮ��
    private bool ClearAllMatchedFruit()
    {
        bool isNeedRefill = false;

        for (int y = 0; y < yRow; y++)
        {
            for (int x = 0; x < xColumn; x++)
            {
                if (Fruits[x, y].canClear())
                {
                    List<GameFruit> matchList = MatchFruit(Fruits[x, y], x, y);

                    if (matchList != null)
                    {
                        // ��������ˮ��
                        FruitType specialFruitType = FruitType.COUNT;

                        GameFruit randomFruit = matchList[Random.Range(0, matchList.Count)];
                        int specialFruitX = randomFruit.X;
                        int specialFruitY = randomFruit.Y;

                        // ��������4
                        if (matchList.Count == 4)
                        {
                            specialFruitType = (FruitType)Random.Range((int)FruitType.ROW_CLEAR, (int)FruitType.COLUMN_CLEAR);
                        }
                        // ������������
                        if (matchList.Count >= 5)
                        {
                            specialFruitType = FruitType.SUPERSTAR;
                        }

                        for (int i = 0; i < matchList.Count; i++)
                        {
                            if (ClearFruit(matchList[i].X, matchList[i].Y))
                            {
                                isNeedRefill = true;
                            }
                        }

                        if (specialFruitType != FruitType.COUNT)
                        {
                            Destroy(Fruits[specialFruitX, specialFruitY]);
                            GameFruit newFruit = CreateNewFruit(specialFruitX, specialFruitY, specialFruitType);
                            if (specialFruitType == FruitType.ROW_CLEAR ||
                                specialFruitType == FruitType.COLUMN_CLEAR &&
                                newFruit.canColor() && matchList[0].canColor())
                            {
                                newFruit.ColoredComponent.SetColor(matchList[0].ColoredComponent.Color);
                            }
                            // ��������
                            else if (specialFruitType == FruitType.SUPERSTAR &&
                                newFruit.canColor())
                            {
                                newFruit.ColoredComponent.SetColor(ColorFrult.ColorType.ANY);
                            }
                        }
                    }
                }
            }
        }

        return isNeedRefill;
    }

    public void ReturnToMain()
    {
        SceneManager.LoadScene(0);
    }

    public void Replay()
    {
        SceneManager.LoadScene(1);
    }

    // 0��ͣ 1��ʼ
    public void Stop()
    {
        if (!isStop)
        {
            AudioSource.PlayClipAtPoint(gameStopAudio, Vector3.zero);
            Time.timeScale = 0;
            gameStopPanel.SetActive(true);
            isStop = true;
        }
        else
        {
            Time.timeScale = 1;
            AudioSource.PlayClipAtPoint(gameStopAudio, Vector3.zero);
            gameStopPanel.SetActive(false);
            isStop = false;
        }
        
    }

    // �ȴ�ǰ��ˮ���������
    private IEnumerator PositonMoveCoroutine(GameFruit fruit1, GameFruit fruit2, int tempX,int tempY,int tempMoved1X,int tempMoved1Y)
    {
        yield return new WaitForSeconds(finishExchangeTime);
        fruit1.MovedComponent.Move(tempX, tempY, fillTime);
        fruit2.MovedComponent.Move(tempMoved1X, tempMoved1Y, fillTime);

        Fruits[fruit1.X, fruit1.Y] = fruit1;
        Fruits[fruit2.X, fruit2.Y] = fruit2;
    }

    // ����� ����ˮ��  
    public void ClearRow(int row)
    {
        for (int x = 0; x < xColumn; x++)
        {
            ClearFruit(x, row);
        }
    }
    // ����� ����ˮ��  
    public void ClearColumn(int column)
    {
        for (int y = 0; y < yRow; y++)
        {
            ClearFruit(column, y);
        }
    }

    // �����ɫ
    public void ClearColoer(ColorFrult.ColorType color)
    {
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                if (Fruits[x,y].canColor() &&
                    (Fruits[x, y].ColoredComponent.Color == color || color == ColorFrult.ColorType.ANY))
                {
                    ClearFruit(x, y);
                }
            }
        }
    }
}
