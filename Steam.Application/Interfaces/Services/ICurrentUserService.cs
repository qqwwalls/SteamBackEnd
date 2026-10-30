using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Interfaces.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAdmin { get; }
}
