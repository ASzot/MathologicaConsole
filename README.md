# Mathologica

Mathologica is a symbolic math evaluation program. You can see it in action at [https://www.mathologica.com/](https://www.mathologica.com/). 

What does it mean to be a symbolic math evaluation program? Mathologica transforms user text input into a series of math symbols that the system can transform and work with. However, that is only part of the functionality. Mathologica is able to determine the meaning of these symbolic expressions and evaluate them accordingly.

## Running
Run this project through Visual Studio. It is the backend of the software that powers the Mathologica website. 

## Notes
I made this project in my Sophomore - Junior year of High School and had while it was certainly a learning experience on handling a big code base I clearly made some bad design choices. The project is fairly large. I also wanted to support automatic compilation between Java and C# code bases. In hindsight I would have just made a backend API but at the time I was devoted to getting offline support. Therefore, that explains some of the weird syntax I used as I wanted syntax that worked with the C# to Java conversion.
