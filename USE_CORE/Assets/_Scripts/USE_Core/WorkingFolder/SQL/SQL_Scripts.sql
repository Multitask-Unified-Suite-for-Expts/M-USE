/*--------------------------Task SCRIPTS ----------------------------------*/

/*Get all Tasks: */
SELECT * FROM Task

/*Get Task by Id (using Id of 17 as example) */
SELECT * FROM Task
WHERE Id = 17;

/*--------------------------SUBJECT SCRIPTS ----------------------------------*/

/*Get all Subjects: */
SELECT * FROM Subject;

/*Get Subject by Id (using Id of 17 as example) */
SELECT * FROM Subject
WHERE Id = 17;

/*--------------------------SUBJECT SCRIPTS ----------------------------------*/

/*Get all Sessions: */
SELECT sesh.*, sub.Name
FROM Session sesh
JOIN Subject sub ON sub.Id=sesh.SubjectId;

/*Get Session by Id (using Id of 17 as example) */
SELECT sesh.*, sub.Name
FROM Session sesh
JOIN Subject sub ON sub.Id=sesh.SubjectId
WHERE sesh.Id = 17;