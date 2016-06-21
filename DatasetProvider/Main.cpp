#include <iostream>
#include <fstream>
#include <string>
#include <vector>

using namespace std;

int main() 
{
	ifstream inputFile("D:\graph.txt");
	ifstream outputFile("D:\graphs\graph.txt");
	string value;
	char user[20];
	char event[20];
	vector<int> users;
	vector<int> events;

	while (inputFile.good())
	{
		getline(inputFile, value, '\n');
		int i = 0;
		int j = 0;
		for (i = 0, j = 0; isdigit(value.at(i)) != 0; i++,j++)
		{
			user[j] = value.at(i);
		}
		user[j] = NULL;
		i++;
		for (j = 0; i < value.length(); i++, j++)
		{
			event[j] = value.at(i);
		}
		event[j] = NULL;

		users.push_back(atoi(user));
		events.push_back(atoi(event));
	}
}
