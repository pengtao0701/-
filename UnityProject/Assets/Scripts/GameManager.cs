using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public struct SaveLoadStruct
{
    // 最高分
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

    // 背景格子
    public GameObject gridPrefab;

    // 填充时间 
    public float fillTime;

    // 需要交换的两个对象 
    private GameFruit pressedFruit;
    private GameFruit enteredFruit;

    public enum FruitType
    {
        EMPTY, //空
        NORMAL, // 正常水果
        BARRIER, // 障碍
        SUPERSTAR,// 超级星星
        ROW_CLEAR, // 行消除
        COLUMN_CLEAR, // 列消除
        COUNT // 标记
    }
    // 创建结构体，序列化 将FruitPrefabMap在视图中显示出来【struct】
    [System.Serializable]
    public struct FruitPrefab
    {
        public FruitType type;
        public GameObject prefab;
    }

    // 根据FruitType 获取对应的prefab
    public Dictionary<FruitType, GameObject> FruitPrefabMap;

    public FruitPrefab[] FruitPrefabs;
    // 移动水果音效
    public AudioClip MoveFruitAudio;
    // 水果数组
    private GameFruit[,] Fruits;

    // 获取UI文本对象
    public Text timeText;

    // 一局游戏时间
    private float gameTime = 60;
    // 是否游戏结束
    private bool gameOver = false;
    // 得分
    public int playerScore;
    // 得分文本
    public Text playerScoreText;
    // 增加分数时间
    private float AddScoreTime;
    // 当前得分
    private float currentScore;
    // 结算界面最终得分
    public Text FinalScoreText;
    // 结算界面最高得分
    public Text FinalBestScoreText;
    // 最好记录文字IMG
    public GameObject BestScoreText;
    // 结算遮罩对象
    public GameObject gameOverPanel;

    // 暂停遮罩对象
    public GameObject gameStopPanel;
    // 暂停
    private bool isStop = false;
    // 暂停/开始音效
    public AudioClip gameStopAudio;

    // 前次交换携程等待
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
        // 结构体FruitPrefabMap实例化
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
                // Quaternion.identity 无旋转
                GameObject gridBox = Instantiate(gridPrefab, FixGridPosition(x, y), Quaternion.identity);
                gridBox.transform.SetParent(transform);
            }
        }

        // 填充空白
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

        // 填充水果
        // 返回携程IEnumerator时用StartCoroutine
        StartCoroutine(AllFill());

    }

    // Update is called once per frame
    void Update()
    {
        if (gameOver)
        {
            // 结束时累计没完成就强制显示当前分数
            if (AddScoreTime != 0f)
            {
                playerScoreText.text = playerScore.ToString();
            }
            return;
        }
        // 时间累减
        gameTime -= Time.deltaTime;

        //游戏结束
        if (gameTime <= 0)
        {
            bool isBestScore = false;

            gameTime = 0;
            // 保存游戏分数
            // 当前分数大于最高分 保存
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
                // 第一次玩算最高分
                PlayerPrefs.SetInt("BestScore", playerScore);
                PlayerPrefs.Save();
                isBestScore = true;
            }

            // 显示结束面板
            // 播放动画
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
            // 测试显示最好成绩
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

    // 修正生成位置
    public Vector3 FixGridPosition(int x, int y)
    {
        return new Vector3(transform.position.x - xColumn / 2f + x,
                                        transform.position.y + yRow / 2f - y,
                                        0);
    }

    // 生成水果
    public GameFruit CreateNewFruit(int x, int y, FruitType type)
    {
        GameObject newFruit = Instantiate(FruitPrefabMap[type], FixGridPosition(x, y), Quaternion.identity);
        newFruit.transform.parent = transform;

        Fruits[x, y] = newFruit.GetComponent<GameFruit>();
        Fruits[x, y].Init(x, y, this, type);

        return Fruits[x, y];
    }

    // 水果填充全部格子
    // IEnumerator 携程
    public IEnumerator AllFill()
    {
        // 是否继续填充
        bool needRefill = true;

        while (needRefill)
        {
            yield return new WaitForSeconds(fillTime);

            while (Fill())
            {
                // 等待fillTime时间继续执行
                yield return new WaitForSeconds(fillTime);
            }

            // 清除已经匹配号的水果
            needRefill = ClearAllMatchedFruit();
        }
    }

    // 单独填充
    public bool Fill()
    {
        // 判断填充是否完成
        bool isFilled = false;

        // 从上往下是0行 -7行
        // 渲染生成是从下网上，所以从下网上遍历,第一行不算 第二行-2开始 
        // 最上面一行是0
        for (int y = yRow - 2; y >= 0; y--)
        {
            for (int x = 0; x < xColumn; x++)
            {
                GameFruit fruit = Fruits[x, y];

                if (fruit.canMove())
                {
                    GameFruit fruitBelow = Fruits[x, y + 1];
                    // 垂直填充
                    // 如果是空 则上方水果向下补充
                    if (fruitBelow.Type == FruitType.EMPTY)
                    {
                        Destroy(fruitBelow.gameObject);
                        fruit.MovedComponent.Move(x, y + 1, fillTime);
                        // 上方水果位置向下一格
                        Fruits[x, y + 1] = fruit;
                        // 因为上方水果向下 ，所以上方换成空，以此循环
                        CreateNewFruit(x, y, FruitType.EMPTY);

                        isFilled = true;
                    }
                    else
                    {
                        // 左右下方填充
                        // -1 左下 1右下
                        for (int down = -1; down <= 1; down++)
                        {
                            if (down != 0)
                            {
                                int downX = x + down;
                                // 排除边界位置
                                if (downX >= 0 && downX < xColumn)
                                {
                                    // y + 1下一行 downX 左边或右边
                                    GameFruit downFruit = Fruits[downX, y + 1];
                                    if (downFruit.Type == FruitType.EMPTY)
                                    {
                                        // 判断是否为垂直填充
                                        bool canFill = true;
                                        for (int aboveY = y; aboveY >= 0; aboveY--)
                                        {
                                            // 垂直方向可以填充 break
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

                                        // 无法垂直填充
                                        // 斜向填充
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

        // 第0行上方隐藏行
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

    // 判断交换时是否相邻
    private bool IsNeighbor(GameFruit fruit1, GameFruit fruit2)
    {
        // 横坐标相同 纵坐标相减绝对值=1 反之
        return (fruit1.X == fruit2.X && Mathf.Abs(fruit1.Y - fruit2.Y) == 1) ||
                (fruit1.Y == fruit2.Y && Mathf.Abs(fruit1.X - fruit2.X) == 1);

    }

    // 水果交换
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
                // 清除水果
                ClearAllMatchedFruit();
                // 填充空位
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
                // 开启携程等待前次交换
                positonMoveCoroutine = PositonMoveCoroutine(fruit1, fruit2, tempX, tempY, tempMoved1X, tempMoved1Y);
                StartCoroutine(positonMoveCoroutine);
                
            }

        }
    }

    // 按下
    public void PressFruit(GameFruit fruit)
    {
        if (gameOver)
        {
            return;
        }
        pressedFruit = fruit;
    }
    // 经过
    public void EnterFruit(GameFruit fruit)
    {
        if (gameOver)
        {
            return;
        }
        enteredFruit = fruit;
    }
    // 松开
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


    // 匹配方法
    public List<GameFruit> MatchFruit(GameFruit fruit, int newX, int newY)
    {
        if (fruit.canColor())
        {
            ColorFrult.ColorType color = fruit.ColoredComponent.Color;
            List<GameFruit> matchRowFruit = new List<GameFruit>();
            List<GameFruit> matchLineFruit = new List<GameFruit>();
            List<GameFruit> finishedMatchFruit = new List<GameFruit>();

            // ----------------------行匹配--------------------------
            matchRowFruit.Add(fruit);

            // 0左 1右
            for (int i = 0; i <= 1; i++)
            {
                for (int xDistance = 1; xDistance < xColumn; xDistance++)
                {
                    int x;
                    if (i == 0)
                    {
                        //向左
                        x = newX - xDistance;
                    }
                    else
                    {
                        //向右
                        x = newX + xDistance;
                    }
                    // 判断边界
                    if (x < 0 || x >= xColumn)
                    {
                        break;
                    }
                    // 满足可设置水果 并且是同一种水果,就存入匹配列表中
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

            // LT匹配 行的列匹配
            // 判断匹配列表里是否大于3个元素
            if (matchRowFruit.Count >= 3)
            {
                //Debug.Log("matchRowFruit.Count= " + matchRowFruit.Count);
                for (int i = 0; i < matchRowFruit.Count; i++)
                {
                    //Debug.Log("i= " + i);
                    // 满足行遍历后每个元素再列遍历 
                    // 0 上 1 下
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


            //------------------ 列匹配-----------------
            matchLineFruit.Add(fruit);

            // 0左 1右
            for (int i = 0; i <= 1; i++)
            {
                for (int yDistance = 1; yDistance < yRow; yDistance++)
                {
                    int y;
                    if (i == 0)
                    {
                        //向左
                        y = newY - yDistance;
                    }
                    else
                    {
                        //向右
                        y = newY + yDistance;
                    }
                    // 判断边界
                    if (y < 0 || y >= yRow)
                    {
                        break;
                    }
                    // 满足可设置水果 并且是同一种水果,就存入匹配列表中
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

            // LT匹配 列的行匹配
            // 判断匹配列表里是否大于3个元素
            if (matchLineFruit.Count >= 3)
            {
                //Debug.Log("matchLineFruit.Count= " + matchLineFruit.Count);
                for (int i = 0; i < matchLineFruit.Count; i++)
                {
                    //Debug.Log("i= " + i);
                    // 满足行遍历后每个元素再列遍历 
                    // 0 上 1 下
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

    // 清除水果
    public bool ClearFruit(int x, int y)
    {
        if (Fruits[x, y].canClear() && !Fruits[x, y].ClearComponent.IsClearing)
        {
            Fruits[x, y].ClearComponent.Clear();
            CreateNewFruit(x, y, FruitType.EMPTY);
            // 清除障碍
            ClearBarrier(x, y);
            return true;
        }

        return false;
    }

    // 清除障碍
    private void ClearBarrier(int x, int y)
    {
        for (int aroundX = x - 1;  aroundX <= x + 1;  aroundX++)
        {
            // 不为自身并且在格子范围内
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
            // 不为自身并且在格子范围内
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

    // 清除全部匹配列表水果
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
                        // 产生特殊水果
                        FruitType specialFruitType = FruitType.COUNT;

                        GameFruit randomFruit = matchList[Random.Range(0, matchList.Count)];
                        int specialFruitX = randomFruit.X;
                        int specialFruitY = randomFruit.Y;

                        // 消除大于4
                        if (matchList.Count == 4)
                        {
                            specialFruitType = (FruitType)Random.Range((int)FruitType.ROW_CLEAR, (int)FruitType.COLUMN_CLEAR);
                        }
                        // 产生超级星星
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
                            // 超级星星
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

    // 0暂停 1开始
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

    // 等待前次水果交换完成
    private IEnumerator PositonMoveCoroutine(GameFruit fruit1, GameFruit fruit2, int tempX,int tempY,int tempMoved1X,int tempMoved1Y)
    {
        yield return new WaitForSeconds(finishExchangeTime);
        fruit1.MovedComponent.Move(tempX, tempY, fillTime);
        fruit2.MovedComponent.Move(tempMoved1X, tempMoved1Y, fillTime);

        Fruits[fruit1.X, fruit1.Y] = fruit1;
        Fruits[fruit2.X, fruit2.Y] = fruit2;
    }

    // 清除行 特殊水果  
    public void ClearRow(int row)
    {
        for (int x = 0; x < xColumn; x++)
        {
            ClearFruit(x, row);
        }
    }
    // 清除列 特殊水果  
    public void ClearColumn(int column)
    {
        for (int y = 0; y < yRow; y++)
        {
            ClearFruit(column, y);
        }
    }

    // 清除颜色
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
