# 🏢 HRMS — Human Resource Management System

A full-featured HR Management System built with **ASP.NET Core MVC**, **Entity Framework Core**, and **SQL Server**. Recognized as one of the **top projects in the cohort** at the German International University (GIU), Cairo.

## About

This HRMS covers the full employee lifecycle — from account creation and attendance tracking to leave management, shift scheduling, performance monitoring, and organizational hierarchy. Built with a focus on security, role-based access, and a polished modern UI.

## Features

### Authentication & Security
- Custom authentication system with session-based login
- Forgot password flow with security question verification
- Account lockout after 5 failed login attempts
- Remember me functionality (30-day cookie)
- Login activity tracking and history (built on the existing Notification table — no new tables required)
- Show/hide password toggle and password strength indicators
- Change password with server-side validation
- Role-based access control on every controller action
- Anti-forgery token validation on all POST requests

### Employee Management
- System admins can create accounts for new employees
- HR admins can edit any part of an employee's profile
- Profile picture upload
- Employee directory with searchable cards
- Profile completeness tracking

### Attendance & Shifts
- Clock in / clock out system
- Attendance correction requests with manager approval
- Shift assignment and management (Morning, Afternoon, Night, Flexible)
- Attendance heatmap and analytics
- HR override logging with timestamps
- Manager flagging system for suspicious patterns

### Leave Management
- 8+ configurable leave types
- Leave request submission with justification
- Multi-level approval workflow
- Leave balance tracking
- Leave pattern analytics and anomaly detection

### Organization & Hierarchy
- Interactive organization chart visualization
- Department management
- ViewOrgHierarchy and ReassignHierarchy stored procedures
- Manager-employee relationship tracking

### Performance & Missions
- Performance dashboard using existing attendance, leave, and mission data
- Employee skills matrix with ratings
- Manager notes and feedback history
- Mission/task tracking with completion rates
- Attendance and punctuality scoring

### Notifications
- In-app notification system with unread count badge
- Real-time notifications for leave approvals, attendance alerts
- Notification preferences

### Calendar
- Interactive calendar view (FullCalendar.js)
- Leave visualization on calendar
- Shift schedule display
- Team availability overview

### Contract Management
- Employment contract tracking
- Contract assignment to employees
- Contract viewing and management (HR Admin)

### Analytics Dashboard
- Chart.js-powered analytics
- Employee distribution by department and role
- Attendance trends and statistics
- Leave utilization reports
- Animated counters with IntersectionObserver

### UI/UX
- Modern gradient-based design with Bootstrap 5
- Dark mode with localStorage persistence
- Toast notifications (Success, Error, Warning, Info)
- Live clock in navbar
- Back-to-top button, scroll effects, page loader animation
- Responsive layout for all screen sizes
- Card-based interface with hover effects and tooltips
- Confirmation dialogs for destructive actions

## Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core MVC |
| Language | C# |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Frontend | Bootstrap 5, JavaScript, Chart.js, FullCalendar.js |
| Icons | Bootstrap Icons |
| Fonts | Google Fonts (Inter) |
| Architecture | MVC with ViewModels, Partial Views, Dependency Injection |

## Project Scale

| Category | Count |
|----------|-------|
| Controllers | 10+ |
| Views | 50+ |
| Database Tables | 20+ |
| API Endpoints | 15+ |
| User Roles | 4 (System Admin, HR Admin, Line Manager, Employee) |
| Leave Types | 8+ (configurable) |
| Shift Types | 4+ |

## User Roles

| Role | Capabilities |
|------|-------------|
| **System Admin** | Full system access, create accounts, manage hierarchy |
| **HR Admin** | Employee profiles, leave/attendance management, contracts, reports |
| **Line Manager** | Team attendance, leave approvals, performance notes, shift management |
| **Employee** | Self-service portal, clock in/out, leave requests, notifications |

## How to Run

1. **Prerequisites** — .NET 6+ SDK, SQL Server, Visual Studio 2022
2. **Clone the repo**
   ```bash
   git clone https://github.com/IsmailSamir1/HRMS-Project.git
   cd HRMS-Project/WebApplication1
   ```
3. **Update the connection string** in `appsettings.json` to point to your SQL Server instance
4. **Run database migrations** or execute the SQL scripts to set up the schema
5. **Run the application**
   ```bash
   dotnet run
   ```
   Or open in Visual Studio and press F5

## Key Design Decisions

- **No new tables for auth features** — forgot password, account lockout, and login activity were all implemented by extending the existing database schema, demonstrating the ability to work within constraints
- **Stored procedures** for complex operations like org hierarchy traversal and reassignment
- **Async/await pattern** throughout all controllers for non-blocking database operations
- **Separation of concerns** with ViewModels for complex data presentation and partial views for reusable components

## What I Learned

- Building enterprise-grade web applications with ASP.NET Core MVC and Entity Framework
- Implementing custom authentication and security layers without Identity framework
- Designing role-based access control across a multi-role system
- Working within database constraints — extending functionality without schema changes
- Integrating third-party libraries (Chart.js, FullCalendar) for rich data visualization
- Building responsive, modern UIs with dark mode support and interactive components

## Context

Built during **Semester 2** (Nov 2025 – Jan 2026) at the **German International University**, Cairo — as part of the Database I course. Recognized as one of the top projects in the cohort.

## License

This project was developed for educational purposes at GIU.
