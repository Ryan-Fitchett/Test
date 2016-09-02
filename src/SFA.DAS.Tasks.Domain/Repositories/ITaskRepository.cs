﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.Tasks.Domain.Entities;
using Task = SFA.DAS.Tasks.Domain.Entities.Task;

namespace SFA.DAS.Tasks.Domain.Repositories
{
    public interface ITaskRepository
    {
        System.Threading.Tasks.Task Create(Task task);

        Task<Task> GetById(long id);

        Task<IList<Task>> GetByAssignee(string assignee);

        System.Threading.Tasks.Task SetCompleted(Task task);

        TaskAlert Create(TaskAlert taskAlert);

        Task<IList<TaskAlert>> GetByUser(string userId);
    }
}
