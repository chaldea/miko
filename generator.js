const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');
const components = require('./components.json');
const slotted1 = /(?: )::slotted\(([\s\S]*?)\)(?!\))/gm;    // match ::slotted
const slotted2 = /(?:\):):slotted\(([\s\S]*?)\)(?!\))/gm;   // match ::slotted
const slotted3 = /::slotted\(([\s\S]*?)\)(?!\))/gm;         // match ::slotted
const host1 = /:host-context\(([\s\S]*?)\)(?!\))/gm;        // match :host-context
const host2 = /:host(?!-)(?:\(([\s\S]*?)\))*(\s|,|::)/gm;   // match :host
const conflict = /^(?=\.)([\s\S]*?)(?: }| |,\n)/gm;
const styles = {};

function transform(name, content) {
    return content
        .replace(slotted1, ` slot $1`)
        .replace(slotted2, `) slot $1`)
        .replace(slotted3, `${name} slot $1`)
        .replace(host1, `$1 ${name}`)
        .replace(host2, `${name}$1$2`)
}

function checkConflict(name, content) {
    const matches = content.matchAll(conflict);
    for (const match of matches) {
        const key = match[1];
        let obj;
        if (!styles[key]) {
            obj = styles[key] = { total: 0 };
        } else {
            obj = styles[key];
        }
        if (!obj[name]) {
            obj.total += 1;
            obj[name] = 1;
        } else {
            obj[name] = obj[name] + 1;
        }
    }
}

function fixConflict(name, content, classNames) {
    let c = content;
    for (const className of classNames) {
        const regex = new RegExp(`^(?=\.)(${className}[\s\S]*?)(?: }| |,\n)`, 'gm');
        c = c.replace(regex, `${name} $1`);
    }
    return c;
}

function listConflictingStyles() {
    const rootPath = './src/components';
    const fileNames = fs.readdirSync(rootPath);
    for (const dir of fileNames) {
        const p = path.resolve(rootPath, dir);
        if (!fs.statSync(p).isDirectory()) continue;
        const files = fs.readdirSync(p);
        for (const file of files) {
            if (path.extname(file) !== '.scss') continue;
            const content = fs.readFileSync(path.resolve(p, file), 'utf8');
            checkConflict(dir, content);
        }
    }
    for (const key in styles) {
        if (styles[key].total > 1) continue;
        delete styles[key];
    }
    console.log(styles);
}

function generateCss() {
    const srcPath = '../../ionic/ionic-framework/core';
    for (const component of components) {
        const componentPath = path.resolve(component.root ? component.root : srcPath, component.srcDir);
        const fileNames = fs.readdirSync(componentPath);
        for (let fileName of fileNames) {
            // 跳过无需处理的文件类型
            if (path.extname(fileName) !== component.include) {
                continue;
            }
            const srcFile = path.resolve(componentPath, fileName);
            let file = fs.readFileSync(srcFile, 'utf8');
            let tag = component.tag;
            let distFile = path.resolve(component.srcDir, fileName);
            const specific = component.specifics.find(x => x.name === fileName);
            if (specific) {
                // 重置文件名
                if (specific.extname) {
                    distFile = distFile.replace(path.extname(distFile), specific.extname);
                }

                // 重置tag
                if (specific.suffix) {
                    tag = `${component.tag}${specific.suffix}`;
                }

                // 转化格式
                if (specific.transform) {
                    file = transform(tag, file);
                }

                if (specific.conflict) {
                    file = fixConflict(tag, file, specific.conflict);
                }
            }
            console.log(distFile);
            fs.writeFileSync(distFile, file);
        }
    }

    const context = {
        path: 'src/css/components.scss',
    }
    execSync(`"./node_modules/.bin/hygen" generator style style --json=${Buffer.from(JSON.stringify(context)).toString('base64')}`);
}

function generateIcons() {
    const context = {
        path: 'src/components/icon/IconResource.cs',
        svgRoot: '../../ionic/ionicons/src/svg'
    }
    execSync(`"./node_modules/.bin/hygen" generator icon icon --json=${Buffer.from(JSON.stringify(context)).toString('base64')}`);
}

function generate() {
    generateCss();
    generateIcons();
}

generate();
