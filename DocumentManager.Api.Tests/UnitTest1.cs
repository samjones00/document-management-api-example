using System;
using DocumentManager.Common;
using DocumentManager.Common.Interfaces;
using DocumentManager.Common.Providers;
using Moq;
using Xunit;

namespace DocumentManager.Api.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            var uploadItemFactory = new Mock<IUploadItemFactory>();

           // var function = new Functions(uploadItemFactory.Object);
        }
    }
}