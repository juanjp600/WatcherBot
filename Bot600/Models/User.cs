using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Bot600.Utils;

namespace Bot600.Models
{
    public class User
    {
        public User(ulong userId, uint totalMessages = 0, uint cringeMessages = 0)
        {
            UserId = userId;
            TotalMessages = totalMessages;
            CringeMessages = cringeMessages;
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
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public ulong UserId { get; private set; }

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
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] 
        public bool IsCringeBool
        {
            get => TotalMessages > 3 && (double) CringeMessages / TotalMessages > 0.5;
            // ReSharper disable once UnusedMember.Local
            // ReSharper disable once ValueParameterNotUsed
            // This is needed for EF core
            private set { }
        }
        
        public IsCringe IsCringe => IsCringeBool ? IsCringe.Yes : IsCringe.No;

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
            if (isCringe == IsCringe.Yes)
            {
                CringeMessages++;
            }
        }
    }
}
