USE [master]

IF db_id('USE_Test') IS NULL
  CREATE DATABASE [USE_Test]
GO
USE [USE_Test]
GO

DROP TABLE IF EXISTS Subject;
DROP TABLE IF EXISTS Task;
DROP TABLE IF EXISTS Session;
GO

CREATE TABLE Subject (
	Id INTEGER NOT NULL PRIMARY KEY IDENTITY(1, 1),
	[Name] VARCHAR(55)
);
GO

CREATE TABLE Task (
	Id INTEGER NOT NULL PRIMARY KEY IDENTITY(1, 1),
	[Name] VARCHAR(55) NOT NULL
);
GO

CREATE TABLE Session (
	Id INTEGER NOT NULL PRIMARY KEY IDENTITY(1, 1),
	SubjectId INTEGER NOT NULL,
	Date DATETIME NOT NULL,
	Comments VARCHAR(255)
);
GO

ALTER TABLE [Session] ADD FOREIGN KEY ([SubjectId]) REFERENCES [Subject] ([Id])
GO


INSERT INTO [Task] ([Name]) VALUES ('ContinuousRecognition');
INSERT INTO [Task] ([Name]) VALUES ('EffortControl');
INSERT INTO [Task] ([Name]) VALUES ('THR');
INSERT INTO [Task] ([Name]) VALUES ('WhatWhenWhere');
INSERT INTO [Task] ([Name]) VALUES ('WorkingMemory');
INSERT INTO [Task] ([Name]) VALUES ('MazeGame');
INSERT INTO [Task] ([Name]) VALUES ('FlexLearning');
INSERT INTO [Task] ([Name]) VALUES ('VisualSearch');
