using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _questionText;

    [SerializeField]
    private Transform _parentAnswerButtonsTransform;
    [SerializeField]
    private Button _buttonPrefab;
    private Button[] _answerButtons;//нужно создавать кнопки

    [SerializeField]
    private TextMeshProUGUI _message;
    [SerializeField, Range(5, 20)]
    private int _countQuestions;

    [Header("Result Panel"), Space(10)]
    [SerializeField]
    private GameObject _resultPanel;

    [SerializeField]
    private TextMeshProUGUI _correctResultText;
    [SerializeField]
    private TextMeshProUGUI _uncorrectResultText;
    [SerializeField]
    private TextMeshProUGUI _resultText;

    [SerializeField]
    private Image _diogram;

    private string _path = "quizzes";

    private int _currentQuestionIndex;
    private int _currentQuestionIndexForQuiz;
    private int[] _mixQuestionsId;
    private QuizList _quizList;

    private int _countCorrectAnswers;
    private void Awake()
    {
        LoadQuizData();
        _currentQuestionIndex = 0;
        _currentQuestionIndexForQuiz = 0;
        _countCorrectAnswers = 0;
        DisplayQuestion();
    }    



    //потом помянять загрузку данных
    //брать для викторины не все вопросы, а например 10 в случайном порядке
    private void LoadQuizData()
    {        
        TextAsset jsonTextAsset = Resources.Load<TextAsset>(_path);
        if (jsonTextAsset)
        {
            string jsonString = jsonTextAsset.text;
            _quizList = JsonUtility.FromJson<QuizList>(jsonString);

            Debug.Log("Успешно");
            Debug.Log(jsonString);
        }

        _mixQuestionsId = RandomMixing(_quizList.quizzes.Length);
    }

    private void DisplayQuestion()
    {
        int questionId = _mixQuestionsId[_currentQuestionIndex];
        _questionText.text = _quizList.quizzes[questionId].question;

        DeleteButtons();
        CreateButtons();

        int answerCount = _quizList.quizzes[questionId].answer.Length;
        int[] answerOrder = new int[answerCount];
        answerOrder = RandomMixing(answerCount);

        for (int i = 0; i < answerCount; i++)
        {
            int answerIndex = answerOrder[i];
            Button button = _answerButtons[i];

            button.GetComponentInChildren<TextMeshProUGUI>().text = _quizList.quizzes[questionId].answer[answerIndex];

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => CheckAnswer(answerIndex, questionId));
        }        
    }
    
    private int[] RandomMixing(int lenght)
    {
        int[] mixId = new int[lenght];

        for (int i = 0; i < mixId.Length; i++)
        {
            mixId[i] = i;
        }

        System.Random random = new System.Random();
        for (int i = 0; i < mixId.Length; i++)
        {
            int j = random.Next(i + 1);
            int temp = mixId[i];
            mixId[i] = mixId[j];
            mixId[j] = temp;
        }

        return mixId;
    }

    private void CreateButtons()
    {
        _answerButtons = new Button[_quizList.quizzes[_currentQuestionIndex].answer.Length];

        for (int i = 0; i < _answerButtons.Length; i++)
        {
            _answerButtons[i] = Instantiate(_buttonPrefab, _parentAnswerButtonsTransform);
        }
    }
    private void DeleteButtons()
    {
        if (_answerButtons == null)
            return;

        for (int i = 0; i < _answerButtons.Length; i++)
        {
            if (_answerButtons[i] != null)
            {
                Destroy(_answerButtons[i].gameObject);
            }
            else
            {
                Debug.LogWarning($"Кнопка {i} уже удалена или не существует");
            }

        }
    }

    private void CheckAnswer(int answerIndex, int questionId)
    {
        if(answerIndex == _quizList.quizzes[questionId].correctAnswerIndex)
        {
            Debug.Log("Молодец");
            StartCoroutine(PrintMessage(Color.green, "Молодец", 3f));
            _countCorrectAnswers++;
        }
        else
        {
            //добавить попыпки за рекламу
            Debug.Log("Попробуй ещё раз");            
            StartCoroutine(PrintMessage(Color.red, "Эх...", 3f));
        }
        _currentQuestionIndex++;
        _currentQuestionIndexForQuiz++;
        if (_currentQuestionIndexForQuiz < _countQuestions)
        {            
            DisplayQuestion();
        }
        else
        {
            if (_countQuestions >_quizList.quizzes.Length - _currentQuestionIndex)
            {
                Debug.Log("Вопросы закончились");
            }
            Debug.Log($"Викторина окончена. Твои резульататы: {_countCorrectAnswers} правильных ответов из {_countQuestions}");

            StartCoroutine(PrintMessage(Color.white,
                $"Викторина окончена. Твои резульататы: {_countCorrectAnswers} правильных ответов из {_countQuestions}",
                10f));

            _resultPanel.SetActive(true);
            PrintResult();            

            _countCorrectAnswers = 0;
            _currentQuestionIndexForQuiz = 0;
        }
    }

    private void SecondQuiz()
    {

    }

    private IEnumerator PrintMessage(Color color, string result, float duration)
    {
        _message.color = color;
        _message.text = result;
        yield return new WaitForSeconds(duration);
        _message.text = "";
    }

    private void PrintResult()
    {
        _correctResultText.text = $"Отвечено правильно: {_countCorrectAnswers}/{_currentQuestionIndexForQuiz}";
        _uncorrectResultText.text = $"Отвечено неправльно: {_currentQuestionIndexForQuiz - _countCorrectAnswers}/{_currentQuestionIndexForQuiz}";

        float result = ((float)_countCorrectAnswers / (float)_currentQuestionIndexForQuiz);


        _resultText.text = $"Итого: {result * 100:F2}%";
        _diogram.fillAmount = result;
    }
}

[System.Serializable]
public class QuizData
{
    public int id;
    public string question;
    public string[] answer;
    public int correctAnswerIndex;
    public string idCountry;
}

[System.Serializable]
public class QuizList
{
    public QuizData[] quizzes;
}
