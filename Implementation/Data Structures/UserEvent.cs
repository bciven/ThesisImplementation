using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class UserEvent
    {
        public string Key => User + "-" + Event;

        public UserEvent(int user, int @event, double utility, double priority)
        {
            User = user;
            Event = @event;
            Utility = utility;
            Priority = priority;
        }

        public UserEvent Copy()
        {
            var ue = new UserEvent(User,Event,Utility,Priority);
            return ue;
        }

        public UserEvent(int user, int @event)
        {
            User = user;
            Event = @event;
        }

        public UserEvent(int user, int @event, double utility)
        {
            User = user;
            Event = @event;
            Utility = utility;
        }

        public UserEvent()
        {

        }

        public void SetUserEvent(string key)
        {
            var values = key.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                User = int.Parse(values[0]);
                Event = int.Parse(values[1]);
            }
            catch (Exception)
            {

                throw new Exception("Corrupt User-Event Key");
            }
        }

        public static UserEvent GetUserEvent(string key, double utility)
        {
            var values = key.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                var userEvent = new UserEvent(int.Parse(values[0]), int.Parse(values[1]), utility);
                return userEvent;
            }
            catch (Exception)
            {

                throw new Exception("Corrupt User-Event Key");
            }
        }

        public static string GetKey(int user, int @event)
        {
            return user + "-" + @event;
        }

        public int User { get; set; }
        public int Event { get; set; }
        public double Utility { get; set; }
        public double Priority { get; set; }
    }
}
