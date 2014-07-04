using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perks;

namespace scbot
{
    public class ConsoleUi
    {
        private readonly Options _options;

        public ConsoleUi(Options options)
        {
            _options = options;
        }

        public string AskQuestion(string question, string @default = null, bool yesno = false)
        {
            if (_options.Common.SimpleMode)
            {
                var simpleAnswer = yesno ? YesOrNo(@default).ToString() : @default;
                return simpleAnswer.IfNotNullOrEmpty() ?? "{" + question + "}";
            }

            var formattedQuestion = question + ": ";
            var output = formattedQuestion;

            if (!string.IsNullOrEmpty(@default))
            {
                if (yesno)
                {
                    output += YesOrNo(@default) ? "(Y/n)" : "(y/N)";
                }
                else
                {
                    output += string.Format("({0}) ", @default);
                }
            }
            else if (yesno)
            {
                output += "(y/n)";
            }

            string answer;

            while (true)
            {
                WriteQuestionMark();
                Console.Write(output);

                var userAnswer = Console.ReadLine();

                // exit app if interrupted
                if (userAnswer == null)
                {
                    Environment.Exit(0);
                }

                answer = userAnswer.IfNotNullOrEmpty() ?? @default;

                if (string.IsNullOrEmpty(answer))
                {
                    Console.WriteLine("ERROR: {0} is required", yesno ? "answer" : question);
                }
                else
                {
                    if (yesno)
                    {
                        answer = YesOrNo(answer).ToString();
                    }

                    var finalAnswer = yesno ? (YesOrNo(answer) ? "yes" : "no") : answer;
                    var prevLineLength = output.Replace(formattedQuestion, "").Length + (userAnswer ?? "").Length;
                    var finalAnswerPadded = finalAnswer.PadRight(finalAnswer.Length + Math.Abs(prevLineLength - finalAnswer.Length), ' ');
                    WriteFinalAnswer(finalAnswerPadded, "[?] ".Length + formattedQuestion.Length);

                    break;
                }
            }

            return answer;
        }

        private void WriteFinalAnswer(string answer, int leftPadding)
        {
            var origCursorLeft = Console.CursorLeft;
            var origCursorTop = Console.CursorTop;

            Console.SetCursorPosition(leftPadding, Console.CursorTop - 1);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(answer);
            Console.ResetColor();

            Console.SetCursorPosition(origCursorLeft, origCursorTop);
        }

        private void WriteQuestionMark()
        {
            Console.Write('[');
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write('?');
            Console.ResetColor();
            Console.Write(']');
            Console.Write(' ');
        }

        private bool YesOrNo(string yesno)
        {
            if (string.Equals(yesno, "y", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(yesno, "yes", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(yesno, "true", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(yesno, "n", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(yesno, "no", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(yesno, "false", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            return false;
        }
    }
}
