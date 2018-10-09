using System;

namespace TaiwuModLoader
{
    /// <summary>
    /// Provides an interface for communicating from the client (target) to the server (injector)
    /// </summary>
    public class ServerInterface : MarshalByRefObject
    {
        /// <summary>
        /// Output multiple messages to console.
        /// </summary>
        /// <param name="messages">Array of messages</param>
        public void OutputMessages(string[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
            {
                Console.WriteLine(messages[i]);
            }
        }

        /// <summary>
        /// Output a message to console.
        /// </summary>
        /// <param name="messages">Array of messages</param>
        public void OutputMessage(string message)
        {
            OutputMessages(new string[] { message });
        }

        /// <summary>
        /// Function to ensure IPC channel is still open
        /// </summary>
        public void Ping()
        {
            // Do nothing
        }
    }
}
