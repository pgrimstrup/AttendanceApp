# Attendance App

This is a simple attendance application built using C# and .NET.
* Back-end uses Entity Framework with a SQL Server database. It is intended to be compatible with Azure SQL Database.
* A couple of front-ends built to test their capabilities and ability to provide the desired user experience.

I am also using the project to learn and experiment with, and to provide the code and a test instance of the app for demonstration/portfolio purposes:
* Different Web UI approaches, including Razor (simple, but server-rendered and not very interactive) and Blazor Web Assemblies (more complex, but very user-interactive).
* Deployment to Azure App Services (Windows) and Azure SQL Database. I am also considering Redis for caching, as data doesn't change once it has imported. After all, the past is history!
* GitHib CI/CD actions for automated testing and deployment.
* .NET 10 (preview) and C# 14 (insiders build).
* Lessons learnt will be applied to a future project that is more complex and has a wider international audience with potential for monetization.

## About Me
My name is Paul. I am a software developer with over 30 years of experience in the industry. I have worked on a wide range of projects, from small web applications to large enterprise systems.
I am always open to new opportunities and I can be contacted via [paul.grimstrup@gmail.com](paul.grimstrup@gmail.com).

## Problem Description
In New Zealand, each pistol shooting member has a legal obligation to attend a minimum of 12 club events per year to maintain their firearms license.

Our gun club, Rifle Rod and Gun Club (Manawatu) Inc, needs a simple attendance application to track member attendance at events. 
The club has around 300 members and holds events multiple times per week. The club has a current swipe-card access system that
allows members to enter and exit the main gate, enter the clubrooms, and records attendance on various ranges.

However, the current system has several limitations:
* It does not provide a way to easily view or manage attendance records. In fact, you need to contact the club secretary to get this information.
* It requires manual matching of swipe-card access events to actual club events and to also to club members, which is time-consuming and error-prone.
* Swipe-card data is recorded in a separate system and is not integrated with the club's membership database.

## Solution
To address these issues, I have developed a simple attendance application that:
* Allows Members to view a simple list of membership number and total number of attendances. This is non-identifying information and is only available through the club website's members-only section.
* Allows Members to log in and view their detailed attendance history. This will use 2-Factor Authentication (2FA) for additional security. Logins are based on the membership email address.
* Imports membership records from the club's existing membership database. This is done manually at present, and we are considering options to automate this in the future.
* Imports swipe-card access records from the existing swipe-card system on a daily basis. This frequency may be increased over time, but is currently constrained by the swipe-card system itself.
* Imports iCalendar data from the club's existing event calendar. This will ensure consistency between the event calendar and the attendance records.

## Features

**Clean Architecture**

The application is built using a clean architecture approach, with separate layers for the presentation, application, domain, and infrastructure. 
This makes it easy to maintain and extend the application over time.

There is an `Attendance.Data` project that contains the AppDbContext and all entity models. This project has no dependencies on any other project.

There is an `Attendance.Services` project that contains all business logic and service integrations. This project has a dependency on `Attendance.Data`, but no other dependencies.
Classes within this project are responsible for importing data from the external systems, managing the event calendar based on recurring events, and calculating attendance statistics
on a per-member basis.

There is an `Attendance.ViewModels` project that contains all ViewModels used by the UI projects. This allows the application to follow a MVVM pattern. Most ViewModel classes 
are composed of an Entity model class and additional properties required for the UI. 
This project has a dependency on `Attendance.Data`, but no other dependencies.

**[Obsolete]** There is an `AttendanceApp` project. This was the initial project created for the UI, built using Razor Pages. It's been a while since I built a Razor Pages app, so I wanted to refresh my skills.
I also wanted to keep the initial version simple, but this ended up being too simple. The kind of user-interactivity I wanted simply can't be achieved using a server-rendered web application
without resorting to JavaScript based framework like Angular or Vue. 
This project has dependencies on `Attendance.Data`, `Attendance.Services` and `Attendance.ViewModels`. 
This application provided me with a good proof-of-concept for Azure App Service deployment.

There is an `AttendanceBlazor` project. This is a newer project that I created to experiment with Blazor Web Assemblies. Since I took a clean architecture approach, I was able to create this project
quite easily, reusing the same data and services projects. The UI provides a much more interactive experience. Authentication and authorization is not yet implemeted.
This project has dependencies on `Attendance.Data`, `Attendance.Services` and `Attendance.ViewModels`.

**Exensibility**

With the Clean Architecture approach, it should be relatively easy to add additional features or to create additional UI projects in the future. 
Currently, the application is specific to our club's needs, but I am considering how it could be adapted for other clubs or organizations with similar requirements,
possibly by extending the database using a tenanted approach.

**Testing**

Currently, no live data has been uploaded to Azure SQL. The plan is to generate some fake test data to upload into test instances of the Azure App Service and Azure SQL Database. 
The live version will not be deployed until I am satisfied that the application is working correctly with authentication and authorization fully implemented.
