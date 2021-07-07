using System.ComponentModel.DataAnnotations;
using System.Linq;
using Bot600.Utils;

namespace Bot600.Models
{
    public class User
    {
        public User(ulong userId, uint totalMessages = 0, uint cringeMessages = 0, bool isCringeBool = false)
        {
            UserId = userId;
            TotalMessages = totalMessages;
            CringeMessages = cringeMessages;
            IsCringeBool = isCringeBool;
        }

        /// <summary>
        ///     Primary key for the database to maintain separation from Discord IDs.
        /// </summary>
        [Key]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public uint Id { get; private set; }

        /// <summary>
        ///     The Discord user ID of this user.
        /// </summary>
        public ulong UserId { get; }

        /// <summary>
        ///     The total number of messages this user has sent while the bot is running.
        /// </summary>
        public uint TotalMessages { get; private set; }

        /// <summary>
        ///     The number of messages the user has sent that are cringe.
        /// </summary>
        public uint CringeMessages { get; private set; }

        /// <summary>
        ///     Whether the user is currently classified as cringe or not.
        /// </summary>
        public bool IsCringeBool { get; private set; }

        public static User GetOrCreateUser(WatcherDatabaseContext db, ulong userId)
        {
            User? user = db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user is not null)
            {
                return user;
            }

            user = new User(userId);
            db.Users.Add(user);

            return user;
        }

        public void NewMessage(IsCringe isCringe)
        {
            TotalMessages++;
            if (isCringe == Utils.IsCringe.Yes)
            {
                CringeMessages++;
            }

            IsCringeBool = TotalMessages > 3 && (double) CringeMessages / TotalMessages > 0.5;
        }

        public IsCringe IsCringe()
        {
            // Recalculate just in case
            IsCringeBool = TotalMessages > 3 && (double) CringeMessages / TotalMessages > 0.5;
            return IsCringeBool ? Utils.IsCringe.Yes : Utils.IsCringe.No;
        }
    }
}
