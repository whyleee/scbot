using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CredentialManagement;
using Perks;
using DialogResult = System.Windows.Forms.DialogResult;

namespace scbot
{
    public class ConsoleUi
    {
        private readonly CredentialStorage _credentialStorage = new CredentialStorage();
        private readonly Options _options;

        public ConsoleUi(Options options)
        {
            _options = options;
        }

        public string AskQuestion(string question, string @default = null, Func<string, bool> validator = null)
        {
            return Ask(question, @default, yesno: false, validator: validator, reply: ConsoleReply);
        }

        public bool AskYesNo(string question, string @default)
        {
            var trueFalse = Ask(question, @default, yesno: true, validator: null, reply: ConsoleReply);
            return bool.Parse(trueFalse);
        }

        public string AskFile(string question, string dialogTitle, string fileFilter = null, string @default = null)
        {
            return Ask(question, @default, yesno: false, validator: null, reply: () => OpenFileReply(dialogTitle, fileFilter));
        }

        private string Ask(string question, string @default, bool yesno, Func<string, bool> validator, Func<string> reply)
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

                var userAnswer = reply();
                answer = userAnswer.IfNotNullOrEmpty() ?? @default;

                if (string.IsNullOrEmpty(answer))
                {
                    Console.WriteLine("ERROR: {0} is required", yesno ? "answer" : question);
                    continue;
                }
                if (validator != null)
                {
                    if (!validator(answer))
                    {
                        Console.WriteLine("ERROR: invalid " + question);
                        continue;
                    }
                }

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

            return answer;
        }

        public bool AskCredentials(Func<string, string, bool> credentialsTest, string title = null,
            string message = null, string username = null, string password = null)
        {
            var loggedIn = false;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                loggedIn = credentialsTest(username, password);
            }

            Credentials credentials = null;

            if (!loggedIn)
            {
                credentials = _credentialStorage.GetSavedCredentials();

                if (credentials != null)
                {
                    loggedIn = credentialsTest(credentials.Username, credentials.Password);
                }
            }

            if (!loggedIn)
            {
                var fillUsername = username.IfNotNullOrEmpty() ?? credentials.Username;
                var fillPassword = password.IfNotNullOrEmpty() ?? credentials.Password;

                using (var prompt = new VistaPrompt())
                {
                    prompt.Title = title;
                    prompt.Message = message;
                    prompt.GenericCredentials = true;
                    prompt.ShowSaveCheckBox = true;

                    if (!string.IsNullOrEmpty(fillUsername))
                    {
                        prompt.Username = fillUsername;
                    }
                    if (!string.IsNullOrEmpty(fillPassword))
                    {
                        prompt.Password = fillPassword;
                    }

                    while (!loggedIn)
                    {
                        var result = prompt.ShowDialog();

                        if (result == CredentialManagement.DialogResult.OK)
                        {
                            loggedIn = credentialsTest(prompt.Username, prompt.Password);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (prompt.SaveChecked)
                    {
                        _credentialStorage.SaveCredentials(prompt.Username, prompt.Password);
                    }
                }
            }

            return loggedIn;
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

        private string ConsoleReply()
        {
            var answer = Console.ReadLine();

            // exit app if interrupted
            if (answer == null)
            {
                Environment.Exit(0);
            }

            return answer;
        }

        private string OpenFileReply(string dialogTitle, string fileFilter)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = dialogTitle;
                dialog.Filter = fileFilter;

                var result = dialog.ShowDialog();
                Console.WriteLine();

                return result == DialogResult.OK ? dialog.FileName : null;
            };
        }
    }
}
