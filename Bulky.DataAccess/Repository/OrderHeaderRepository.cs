using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderHeader orderHeader)
        {
            _db.OrderHeaders.Add(orderHeader);
        }

        public void UpdateStatus(int id, string orderStatus, string? PaymentStatus = null)
        {
            var orderFromDb=_db.OrderHeaders.FirstOrDefault(u=>u.Id == id);

            if(orderFromDb is not null)
            {
                orderFromDb.OrderStatus = orderStatus;
                if (!string.IsNullOrEmpty(PaymentStatus))
                {
                    orderFromDb.PaymentStatus = PaymentStatus;
                }
            }
        }

        public void UpdateStripePaymentID(int id, string sessionId, string PaymentIntentId)
        {
            var orderFromDb=_db.OrderHeaders.FirstOrDefault(u=>u.Id == id);

            if(!string.IsNullOrEmpty(sessionId))
            {
                orderFromDb.SessionId = sessionId;
            }
            if(!string.IsNullOrEmpty(PaymentIntentId))
            {
                orderFromDb.PaymentIntentId = PaymentIntentId;
                orderFromDb.PaymentDate = DateTime.Now;
            }
        }
    }
}
