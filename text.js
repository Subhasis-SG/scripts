const fs = require('fs');

// Read the file
fs.readFile('test.txt', 'utf8', (err, data) => {
    if (err) throw err;

    // Regex to match False Positive entries
    const pattern = /--- False Positive \d+:/g;

    // Replace matches with incrementing numbers
    let counter = 1;
    const updatedData = data.replace(pattern, () => `--- False Positive ${counter++}:`);

    // Write back to a new file
    fs.writeFile('test_updated.txt', updatedData, (err) => {
        if (err) throw err;
        console.log('File updated successfully!');
    });
});
