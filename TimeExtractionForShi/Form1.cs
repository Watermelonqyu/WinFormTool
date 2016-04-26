using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;

using System.Web.Script.Serialization;
using CSALMongo.Model;
using CSALMongo.TurnModel;
using CSALMongo;

namespace TimeExtractionForShi
{
    public partial class Form1 : Form
    {
        //should change the name of the database
        public const string DB_URL = "mongodb://localhost:27017/csaldata";

        //protected MongoDatabase testDB = null;
        List<String> studentsInClass = new List<string>();
        List<String> studentB = new List<string>();
        public Form1()
        {
            InitializeComponent();

            String folderPath = "D:\\CSAL\\Shi\\TimeExtractionForShi\\Log files\\Log";

            List<string> allData = new List<string>();

            foreach (string file in Directory.EnumerateFiles(folderPath, "*.txt"))
            {

                var logFile = File.ReadAllLines(file);
                List<string> LogList = new List<string>(logFile);
                LogList.RemoveAt(2);
                string contents = "";
                foreach (string line in LogList)
                {
                    contents += line;
                }

                string userIDTxt = Path.GetFileName(file);
                string userID = userIDTxt.Split('.')[0];
                studentB.Add(userID);
                //var serializer = new JavaScriptSerializer();
                // StudentLessonActs studentAct = serializer.Deserialize<StudentLessonActs>(contents.ToString());
                //StudentLessonActs studentAct = Newtonsoft.Json.JsonConvert.DeserializeObject<StudentLessonActs>(contents);
            }            
            
            
            // Start 
            try
            {
                //query from the database
                var db = new CSALDatabase(DB_URL);
                var students = db.FindStudents();
                var lessons = db.FindLessons();
                var classes = db.FindClasses();

                List<string> needStudentsA = new List<string>();
                // find the target classes
                foreach (var oneClass in classes)
                {
                    //ace | kingwilliam | main | tlp
                    if (oneClass.ClassID == "shifeng")
                    {

                        foreach (String student in oneClass.Students)
                        {
                            if (!String.IsNullOrWhiteSpace(student) && !String.IsNullOrWhiteSpace(oneClass.ClassID))
                            {
                                String cS = oneClass.ClassID + "-" + student;
                                needStudentsA.Add(cS);
                            }

                        }
                    }
                }
                List<String> oneStudent = new List<string>();

                foreach (var student in needStudentsA)
                {
                    String classID = student.Split(new Char[] { '-' })[0];
                    String subjectID = student.Split(new Char[] { '-' })[1];
                    if (subjectID == "shi")
                    {
                        continue;
                    }

                    for (int i = 1; i < 4; i++)
                    {
                        String lessonID = "lesson";
                        lessonID += i.ToString();
                        var oneTurn = db.FindTurns(lessonID, subjectID);
                        if (oneTurn == null || oneTurn.Count < 1 || oneTurn[0].Turns.Count < 1)
                        {
                            continue;
                        }

                        oneStudent = getRecords(oneTurn[0]);

                        if (oneStudent == null || oneStudent.Count < 1) {
                            continue;
                        }

                        foreach (var one in oneStudent)
                        {
                            String fullRecord = subjectID + "\t" + lessonID + "\t" + one;
                            allData.Add(fullRecord);
                        }
                    }
                    
                }

                foreach(var studentName in studentB) {
                    
                    for (int i = 1; i < 4; i++)
                    {
                        String lessonID = "lesson";
                        lessonID += i.ToString();
                        var oneTurn = db.FindTurns(lessonID, studentName);
                        if (oneTurn == null || oneTurn.Count < 1 || oneTurn[0].Turns.Count < 1)
                        {
                            continue;
                        }

                        oneStudent = getRecords(oneTurn[0]);

                        if (oneStudent == null || oneStudent.Count < 1) {
                            continue;
                        }

                        foreach (var one in oneStudent)
                        {
                            String fullRecord = studentName + "\t" + lessonID + "\t" + one;
                            allData.Add(fullRecord);
                        }
                    }
                    
                }  
        
                int recordID = 0;
                foreach (String perRecord in allData)
                {
                    recordID = recordID + 1;
                    /*
                    String record1 = perRecord.Split(new String[] { "\n" }, StringSplitOptions.None)[0];
                    String record2 = perRecord.Split(new string[] { "\n" }, StringSplitOptions.None)[1];
                    this.richTextBox1.AppendText(recordID.ToString() + "\t" + record1 + "\n" + (recordID+1).ToString() + "\t" + record2 + "\n");
                    */
                    this.richTextBox1.AppendText(recordID.ToString() + "\t" + perRecord + "\n");
                }
            }
            catch(Exception e) {
                e.GetBaseException();
            }
        }
       

        // lesson 1
        public List<String> getRecords(StudentLessonActs log)
        {
            List<String> allrecords = new List<string>();
            List<String> allrecordsOneAttempt = new List<string>();
            int lastTurnID = 99, attempCount = 0, thisAttempCount = 0;
            double duration = 0;
            
            List<double> attempTime = new List<double>();

            
            if (log == null  || log.Turns.Count < 1)
            {
                return null;
            }
            else
            {
                // calculate total time of every Attempt
                foreach (var turn in log.Turns)
                {
                    if (turn.TurnID < lastTurnID)
                    {
                        attempCount++;
                        double turnDura = (int)turn.Duration;
                        turnDura = turnDura / 1000;
                        attempTime.Add(turnDura);
                    }
                    else
                    {
                        double turnDura = (int)turn.Duration;
                        attempTime[attempCount - 1] += turnDura / 1000;
                    }
                    lastTurnID = turn.TurnID;
                }

                var lastTurn = log.Turns[0];

                lastTurnID = -1;
                foreach (var turn in log.Turns)
                {
                    String oneRecord = "";
                    // student tried more than 1, reset everything
                    if (turn.TurnID <= lastTurnID)
                    {
                        if (allrecordsOneAttempt.Count < 1)
                        {
                            continue;
                        }
                        
                        foreach (var record in allrecordsOneAttempt)
                        {
                            String recordDura = attempTime[thisAttempCount] + "\t" + record;
                            allrecords.Add(recordDura);
                        }
                        thisAttempCount++;
                        lastTurnID = 0;
                    }
                    else
                    {
                        
                        if (turn.Input.Event.ToString().Contains("contradictory"))
                        {
                            duration = (int)turn.Duration;
                            duration = duration / 1000;

                            var startDt = new DateTime(2010, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(lastTurn.DBTimestamp / 1000d)).ToLocalTime();

                            var endDt = startDt.AddSeconds(lastTurn.Duration / 1000d).ToLocalTime();
                            string ruleName = "";
                            foreach(var trans in turn.Transitions) {
                                ruleName = trans.RuleID;
                            }
                            oneRecord = ruleName + "\t" + turn.Input.Event + "\t" + startDt + "\t" + endDt + "\t" + duration;
                            allrecordsOneAttempt.Add(oneRecord);
                        }
                        lastTurn = turn;
                        lastTurnID = turn.TurnID;
                    } 
                }
                if (allrecordsOneAttempt.Count < 1)
                {
                    return null;
                }

                foreach (var record in allrecordsOneAttempt)
                {
                    String recordDura = attempTime[attempTime.Count-1] + "\t" + record;
                    allrecords.Add(recordDura);
                }
            }
            return allrecords;
        }

    }
}
