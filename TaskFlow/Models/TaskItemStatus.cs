using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

// Kanban panosundaki sütunlar
public enum TaskItemStatus
{
    [Display(Name = "Yapılacak")]
    Todo = 0,

    [Display(Name = "Devam Ediyor")]
    InProgress = 1,

    [Display(Name = "Tamamlandı")]
    Done = 2
}
