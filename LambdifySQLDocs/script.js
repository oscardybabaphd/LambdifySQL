// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeCodeHighlighting();
    initializeClipboard();
    initializeSmoothScrolling();
    initializeLineNumbers();
    initializeMobileNavigation();
    initializeSidebarNavigation();
});

// Initialize code syntax highlighting
function initializeCodeHighlighting() {
    // Prism.js will automatically highlight code blocks
    // Custom highlighting for line numbers
    const codeBlocks = document.querySelectorAll('pre code');
    codeBlocks.forEach(block => {
        // Add line numbers class if not already present
        if (!block.classList.contains('line-numbers')) {
            block.classList.add('line-numbers');
        }
    });
}

// Initialize clipboard functionality
function initializeClipboard() {
    // Initialize clipboard.js for copy buttons
    if (typeof ClipboardJS !== 'undefined') {
        const clipboard = new ClipboardJS('.copy-btn');
        
        clipboard.on('success', function(e) {
            const btn = e.trigger;
            const originalIcon = btn.innerHTML;
            
            // Show success feedback
            btn.innerHTML = '<i class="fas fa-check"></i>';
            btn.style.color = '#10b981';
            
            // Reset after 2 seconds
            setTimeout(() => {
                btn.innerHTML = originalIcon;
                btn.style.color = '';
            }, 2000);
            
            e.clearSelection();
        });
        
        clipboard.on('error', function(e) {
            console.error('Copy failed:', e);
            
            const btn = e.trigger;
            const originalIcon = btn.innerHTML;
            
            // Show error feedback
            btn.innerHTML = '<i class="fas fa-times"></i>';
            btn.style.color = '#ef4444';
            
            // Reset after 2 seconds
            setTimeout(() => {
                btn.innerHTML = originalIcon;
                btn.style.color = '';
            }, 2000);
        });
    }
}

// Initialize smooth scrolling for anchor links
function initializeSmoothScrolling() {
    const links = document.querySelectorAll('a[href^="#"]');
    
    links.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href').substring(1);
            const targetElement = document.getElementById(targetId);
            
            if (targetElement) {
                const offsetTop = targetElement.offsetTop - 80; // Account for fixed navbar
                
                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
                
                // Update URL without jumping
                history.pushState(null, null, `#${targetId}`);
            }
        });
    });
}

// Initialize line numbers for code blocks
function initializeLineNumbers() {
    const codeBlocks = document.querySelectorAll('pre code.line-numbers');
    
    codeBlocks.forEach(block => {
        const lines = block.textContent.split('\n');
        let lineNumbersHtml = '';
        
        for (let i = 1; i <= lines.length; i++) {
            lineNumbersHtml += `<span class="line-number">${i}</span>`;
        }
        
        // Create line numbers container
        const lineNumbersContainer = document.createElement('div');
        lineNumbersContainer.className = 'line-numbers-rows';
        lineNumbersContainer.innerHTML = lineNumbersHtml;
        
        // Insert line numbers container
        const pre = block.parentElement;
        if (pre.tagName === 'PRE' && !pre.querySelector('.line-numbers-rows')) {
            pre.appendChild(lineNumbersContainer);
        }
    });
}

// Initialize mobile navigation
function initializeMobileNavigation() {
    const navToggle = document.querySelector('.nav-toggle');
    const sidebar = document.querySelector('.sidebar');
    const mainContent = document.querySelector('.main-content');
    
    if (navToggle && sidebar) {
        navToggle.addEventListener('click', function() {
            sidebar.classList.toggle('sidebar-open');
            this.classList.toggle('nav-toggle-open');
        });
        
        // Close sidebar when clicking outside
        document.addEventListener('click', function(e) {
            if (!sidebar.contains(e.target) && !navToggle.contains(e.target)) {
                sidebar.classList.remove('sidebar-open');
                navToggle.classList.remove('nav-toggle-open');
            }
        });
        
        // Close sidebar when clicking on a link (mobile)
        const sidebarLinks = sidebar.querySelectorAll('a');
        sidebarLinks.forEach(link => {
            link.addEventListener('click', function() {
                if (window.innerWidth <= 1024) {
                    sidebar.classList.remove('sidebar-open');
                    navToggle.classList.remove('nav-toggle-open');
                }
            });
        });
    }
}

// Initialize sidebar navigation highlighting
function initializeSidebarNavigation() {
    const sections = document.querySelectorAll('section[id]');
    const sidebarLinks = document.querySelectorAll('.sidebar a[href^="#"]');
    
    function highlightCurrentSection() {
        const scrollPosition = window.scrollY + 100; // Offset for navbar
        
        sections.forEach(section => {
            const sectionTop = section.offsetTop;
            const sectionHeight = section.offsetHeight;
            const sectionId = section.getAttribute('id');
            
            if (scrollPosition >= sectionTop && scrollPosition < sectionTop + sectionHeight) {
                // Remove active class from all links
                sidebarLinks.forEach(link => {
                    link.classList.remove('active');
                });
                
                // Add active class to current section link
                const currentLink = document.querySelector(`.sidebar a[href="#${sectionId}"]`);
                if (currentLink) {
                    currentLink.classList.add('active');
                }
            }
        });
    }
    
    // Throttle scroll events
    let scrollTimeout;
    window.addEventListener('scroll', function() {
        if (scrollTimeout) {
            clearTimeout(scrollTimeout);
        }
        scrollTimeout = setTimeout(highlightCurrentSection, 10);
    });
    
    // Initial highlight
    highlightCurrentSection();
}

// Utility function to add active state to sidebar links
function addSidebarActiveStates() {
    const style = document.createElement('style');
    style.textContent = `
        .sidebar a.active {
            background: var(--bg-tertiary);
            color: var(--primary);
            font-weight: 600;
        }
        
        @media (max-width: 1024px) {
            .sidebar {
                transform: translateX(-100%);
                transition: transform 0.3s ease-in-out;
            }
            
            .sidebar.sidebar-open {
                transform: translateX(0);
            }
            
            .nav-toggle.nav-toggle-open span:nth-child(1) {
                transform: rotate(45deg) translate(5px, 5px);
            }
            
            .nav-toggle.nav-toggle-open span:nth-child(2) {
                opacity: 0;
            }
            
            .nav-toggle.nav-toggle-open span:nth-child(3) {
                transform: rotate(-45deg) translate(7px, -6px);
            }
        }
    `;
    document.head.appendChild(style);
}

// Initialize enhanced features
function initializeEnhancedFeatures() {
    // Add keyboard navigation
    document.addEventListener('keydown', function(e) {
        // Press 'S' to focus search (if implemented)
        if (e.key === 's' && !e.ctrlKey && !e.metaKey && e.target.tagName !== 'INPUT') {
            e.preventDefault();
            // Focus search input if available
            const searchInput = document.querySelector('.search-input');
            if (searchInput) {
                searchInput.focus();
            }
        }
        
        // Escape to close mobile menu
        if (e.key === 'Escape') {
            const sidebar = document.querySelector('.sidebar');
            const navToggle = document.querySelector('.nav-toggle');
            if (sidebar && navToggle) {
                sidebar.classList.remove('sidebar-open');
                navToggle.classList.remove('nav-toggle-open');
            }
        }
    });
    
    // Add scroll to top functionality
    const scrollToTopBtn = createScrollToTopButton();
    document.body.appendChild(scrollToTopBtn);
    
    // Show/hide scroll to top button
    window.addEventListener('scroll', function() {
        if (window.scrollY > 500) {
            scrollToTopBtn.classList.add('show');
        } else {
            scrollToTopBtn.classList.remove('show');
        }
    });
}

// Create scroll to top button
function createScrollToTopButton() {
    const button = document.createElement('button');
    button.className = 'scroll-to-top';
    button.innerHTML = '<i class="fas fa-arrow-up"></i>';
    button.setAttribute('aria-label', 'Scroll to top');
    
    button.addEventListener('click', function() {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
    
    // Add styles
    const style = document.createElement('style');
    style.textContent = `
        .scroll-to-top {
            position: fixed;
            bottom: 2rem;
            right: 2rem;
            width: 3rem;
            height: 3rem;
            background: var(--primary);
            color: white;
            border: none;
            border-radius: 50%;
            box-shadow: var(--shadow-lg);
            cursor: pointer;
            opacity: 0;
            visibility: hidden;
            transition: all 0.3s ease-in-out;
            z-index: 1000;
        }
        
        .scroll-to-top:hover {
            background: var(--primary-dark);
            transform: translateY(-2px);
        }
        
        .scroll-to-top.show {
            opacity: 1;
            visibility: visible;
        }
        
        @media (max-width: 768px) {
            .scroll-to-top {
                bottom: 1rem;
                right: 1rem;
                width: 2.5rem;
                height: 2.5rem;
            }
        }
    `;
    
    if (!document.querySelector('style[data-scroll-to-top]')) {
        style.setAttribute('data-scroll-to-top', 'true');
        document.head.appendChild(style);
    }
    
    return button;
}

// Add loading animation for code blocks
function addCodeLoadingAnimation() {
    const codeBlocks = document.querySelectorAll('.code-editor, .output-editor');
    
    codeBlocks.forEach(block => {
        block.style.opacity = '0';
        block.style.transform = 'translateY(20px)';
        block.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
        
        // Animate in when in viewport
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1 });
        
        observer.observe(block);
    });
}

// Initialize all enhanced features
document.addEventListener('DOMContentLoaded', function() {
    addSidebarActiveStates();
    initializeEnhancedFeatures();
    addCodeLoadingAnimation();
});

// Add print functionality
function initializePrintStyles() {
    const printButton = document.createElement('button');
    printButton.className = 'print-btn';
    printButton.innerHTML = '<i class="fas fa-print"></i> Print';
    printButton.style.cssText = `
        position: fixed;
        top: 5rem;
        right: 2rem;
        padding: 0.5rem 1rem;
        background: var(--bg-primary);
        border: 1px solid var(--border);
        border-radius: var(--radius);
        cursor: pointer;
        font-size: 0.875rem;
        z-index: 1000;
        display: none;
    `;
    
    printButton.addEventListener('click', function() {
        window.print();
    });
    
    // Show print button on larger screens
    if (window.innerWidth > 1024) {
        document.body.appendChild(printButton);
        printButton.style.display = 'block';
    }
}

// Initialize print functionality
document.addEventListener('DOMContentLoaded', initializePrintStyles);
